/*
 * Copyright 2019 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


/**
 * A GeoQuery object can be used for geo queries in a given circle. The GeoQuery class is thread safe.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GeoFire.Core;
using GeoFire.Util;
using Plugin.CloudFirestore;

namespace GeoFire
{
    public class GeoQuery
    {
        private readonly object _lock = new object();

        private const int KilometerToMeter = 1000;

        private class LocationInfo
        {
            public GeoPoint Location { get; }
            public bool InGeoQuery { get; }
            public GeoHash GeoHash { get; }
            public IDocumentSnapshot Snapshot { get; }

            public LocationInfo(GeoPoint location, bool inGeoQuery, IDocumentSnapshot snapshot)
            {
                Location = location;
                InGeoQuery = inGeoQuery;
                GeoHash = new GeoHash(location);
                Snapshot = snapshot;
            }
        }

        private readonly GeoFire _geoFire;
        private readonly HashSet<IGeoQueryDataEventListener> _eventListeners = new HashSet<IGeoQueryDataEventListener>();
        private readonly Dictionary<GeoHashQuery, IQuery> _firebaseQueries = new Dictionary<GeoHashQuery, IQuery>();
        private readonly Dictionary<GeoHashQuery, IListenerRegistration> _queryListeners = new Dictionary<GeoHashQuery, IListenerRegistration>();
        private readonly HashSet<GeoHashQuery> _outstandingQueries = new HashSet<GeoHashQuery>();
        private readonly Dictionary<string, LocationInfo> _locationInfos = new Dictionary<string, LocationInfo>();
        private GeoPoint _center;
        private double _radius;
        private HashSet<GeoHashQuery> _queries;

        public GeoQuery(GeoFire geoFire, GeoPoint center, double radius)
        {
            _geoFire = geoFire;
            _center = center;
            _radius = radius * KilometerToMeter; // Convert from kilometers to meters.
        }

        private bool LocationIsInQuery(GeoPoint location)
        {
            return GeoUtils.Distance(location, _center) <= _radius;
        }

        private void UpdateLocationInfo(IDocumentSnapshot document, GeoPoint location)
        {
            var key = document.Id;
            var oldInfo = _locationInfos[key];
            var isNew = oldInfo == null;
            var changedLocation = oldInfo != null && !Equals(oldInfo.Location, location);
            var wasInQuery = oldInfo != null && oldInfo.InGeoQuery;

            var isInQuery = LocationIsInQuery(location);
            if ((isNew || !wasInQuery) && isInQuery)
            {
                foreach (var listener in _eventListeners) {
                    _geoFire.RaiseEvent(() => listener.OnDocumentEntered(document, location));
                }
            }
            else if (!isNew && isInQuery)
            {
                foreach (var listener in _eventListeners)
                {
                    _geoFire.RaiseEvent(() =>
                    {
                        if (changedLocation)
                        {
                            listener.OnDocumentMoved(document, location);
                        }
                        listener.OnDocumentChanged(document, location);
                    });
                }
            }
            else if (wasInQuery && !isInQuery)
            {
                foreach (var listener in _eventListeners)
                {
                    _geoFire.RaiseEvent(r: () => listener.OnDocumentExited(document));
                }
            }

            var newInfo = new LocationInfo(location, LocationIsInQuery(location), document);
            _locationInfos.Add(key, newInfo);
        }

        private bool GeoHashQueriesContainGeoHash(GeoHash geoHash)
        {
            return _queries != null && _queries.Any(query => query.ContainsGeoHash(geoHash));
        }

        private void Reset()
        {
            foreach (var entry in _firebaseQueries)
            {
                _queryListeners[entry.Key].Remove();
            }
            _outstandingQueries.Clear();
            _firebaseQueries.Clear();
            _queryListeners.Clear();
            _queries = null;
            _locationInfos.Clear();
        }

        private bool HasListeners()
        {
            return _eventListeners.Any();
        }

        private bool CanFireReady()
        {
            return !_outstandingQueries.Any();
        }

        private void CheckAndFireReady()
        {
            if (!CanFireReady()) return;
            
            foreach (var listener in _eventListeners)
            {
                _geoFire.RaiseEvent(() => listener.OnGeoQueryReady());
            }
        }
        
        private void SetupQueries()
        {
            var oldQueries = _queries ?? new HashSet<GeoHashQuery>();
            var newQueries = GeoHashQuery.QueriesAtLocation(_center, _radius);
            _queries = newQueries;
            foreach (var query in oldQueries.Where(query => !newQueries.Contains(query)))
            {
                _firebaseQueries.Remove(query);
                _outstandingQueries.Remove(query);
            }
            foreach (var query in newQueries.Where(query => !oldQueries.Contains(query)))
            {
                _outstandingQueries.Add(query);
                var collection = _geoFire.GetCollection();
                var firebaseQuery = collection.OrderBy("g").StartAt(query.GetStartValue())
                    .EndAt(query.GetEndValue());
                _queryListeners.Add(query, firebaseQuery.AddSnapshotListener((snapshot, e) =>
                {
                    if (e != null)
                    {
                        foreach (var listener in _eventListeners) 
                        {
                            _geoFire.RaiseEvent(() => listener.OnGeoQueryError(e));
                        }
                        return;
                    }
                    lock (_lock)
                    {
                        var firQuery = _firebaseQueries.First(x => x.Value == snapshot.Query).Key;
                        _outstandingQueries.Remove(firQuery);
                        CheckAndFireReady();
                    }

                    foreach (var change in snapshot.DocumentChanges)
                    {
                        switch (change.Type)
                        {
                            case DocumentChangeType.Added:
                                ChildAdded(change.Document);
                                break;
                            case DocumentChangeType.Removed:
                                ChildRemoved(change.Document);
                                break;
                            case DocumentChangeType.Modified:
                                ChildChanged(change.Document);
                                break;
                        }
                    }
                }));
                _firebaseQueries.Add(query, firebaseQuery);
            }
            
            foreach (var oldLocationInfo in _locationInfos.Select(info => info.Value).Where(oldLocationInfo => oldLocationInfo != null))
            {
                UpdateLocationInfo(oldLocationInfo.Snapshot, oldLocationInfo.Location);
            }
            // remove locations that are not part of the geo query anymore
            foreach (var entry in _locationInfos.Where(x => !GeoHashQueriesContainGeoHash(x.Value.GeoHash)))
            {
                _locationInfos.Remove(entry.Key);
            }
            
            CheckAndFireReady();
        }

        private void ChildAdded(IDocumentSnapshot document)
        {
            var location = GeoFire.GetLocationValue(document);
            if (location != null)
            {
                UpdateLocationInfo(document, location);
            }
            else
            {
                Debug.Assert(false, "Got Datasnapshot without location with key " + document.Id);
            }
        }

        private void ChildChanged(IDocumentSnapshot document)
        {
            var location = GeoFire.GetLocationValue(document);
            if (location != null)
            {
                UpdateLocationInfo(document, location);
            }
            else
            {
                Debug.Assert(false, "Got Datasnapshot without location with key " + document.Id);
            }
        }

        private void ChildRemoved(IDocumentSnapshot document)
        {
            var key = document.Id;
            var info = _locationInfos[key];
            if (info == null) return;
            lock (_lock)
            {
                var location = GeoFire.GetLocationValue(document);
                var hash = (location != null) ? new GeoHash(location) : null;
                if (hash != null && GeoHashQueriesContainGeoHash(hash)) return;
                
                _locationInfos.Remove(key);
                
                if (!info.InGeoQuery) return;
                
                foreach (var listener in _eventListeners)
                {
                    _geoFire.RaiseEvent(() => listener.OnDocumentExited(info.Snapshot));
                }
            }

        }

        /**
         * Adds a new GeoQueryEventListener to this GeoQuery.
         *
         * @throws IllegalArgumentException If this listener was already added
         *
         * @param listener The listener to add
         */
        public void AddGeoQueryEventListener(IGeoQueryEventListener listener)
        {
            lock (_lock)
            {
                AddGeoQueryDataEventListener(new EventListenerBridge(listener));
            }
        }

        /**
         * Adds a new GeoQueryEventListener to this GeoQuery.
         *
         * @throws IllegalArgumentException If this listener was already added
         *
         * @param listener The listener to add
         */
        public void AddGeoQueryDataEventListener(IGeoQueryDataEventListener listener)
        {
            lock (_lock)
            {
                if (_eventListeners.Contains(listener))
                {
                    throw new ArgumentException("Added the same listener twice to a GeoQuery!");
                }

                _eventListeners.Add(listener);
                if (_queries == null)
                {
                    SetupQueries();
                }
                else
                {
                    foreach (var info in _locationInfos.Select(entry => entry.Value).Where(info => info.InGeoQuery))
                    {
                        _geoFire.RaiseEvent(() => listener.OnDocumentEntered(info.Snapshot, info.Location));
                    }

                    if (CanFireReady())
                    {
                        _geoFire.RaiseEvent(listener.OnGeoQueryReady);
                    }
                }
            }
        }

        /**
         * Removes an event listener.
         *
         * @throws IllegalArgumentException If the listener was removed already or never added
         *
         * @param listener The listener to remove
         */
        public void RemoveGeoQueryEventListener(IGeoQueryEventListener listener)
        {
            lock (_lock)
            {
                RemoveGeoQueryEventListener(new EventListenerBridge(listener));
            }
        }

        /**
         * Removes an event listener.
         *
         * @throws IllegalArgumentException If the listener was removed already or never added
         *
         * @param listener The listener to remove
         */
        public void RemoveGeoQueryEventListener(IGeoQueryDataEventListener listener)
        {
            lock (_lock)
            {
                if (!_eventListeners.Contains(listener))
                {
                    throw new ArgumentException("Trying to remove listener that was removed or not added!");
                }

                _eventListeners.Remove(listener);
                if (!HasListeners())
                {
                    Reset();
                }   
            }
        }

        /**
         * Removes all event listeners from this GeoQuery.
         */
        public void RemoveAllListeners()
        {
            lock (_lock)
            {
                _eventListeners.Clear();
                Reset();   
            }
        }

        /**
         * Returns the current center of this query.
         * @return The current center
         */
        public GeoPoint GetCenter()
        {
            return _center;
        }

        /**
         * Sets the new center of this query and triggers new events if necessary.
         * @param center The new center
         */
        public void SetCenter(GeoPoint center)
        {
            lock (_lock)
            {
                _center = center;
                if (HasListeners())
                {
                    SetupQueries();
                }   
            }
        }

        /**
         * Returns the radius of the query, in kilometers.
         * @return The radius of this query, in kilometers
         */
        public double GetRadius()
        {
            // convert from meters
            return _radius / KilometerToMeter;
        }

        /**
         * Sets the radius of this query, in kilometers, and triggers new events if necessary.
         * @param radius The radius of the query, in kilometers. The Maximum radius that is
         * supported is about 8587km. If a radius bigger than this is passed we'll cap it.
         */
        public void SetRadius(double radius)
        {
            lock (_lock)
            {
                // convert to meters
                _radius = GeoUtils.CapRadius(radius) * KilometerToMeter;
                if (HasListeners())
                {
                    SetupQueries();
                }   
            }
        }

        /**
         * Sets the center and radius (in kilometers) of this query, and triggers new events if necessary.
         * @param center The new center
         * @param radius The radius of the query, in kilometers. The Maximum radius that is
         * supported is about 8587km. If a radius bigger than this is passed we'll cap it.
         */
        public void SetLocation(GeoPoint center, double radius)
        {
            lock (_lock)
            {
                _center = center;
                // convert radius to meters
                _radius = GeoUtils.CapRadius(radius) * KilometerToMeter;
                if (HasListeners())
                {
                    SetupQueries();
                }   
            }
        }
    }
}
