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
 * GeoQuery notifies listeners with this interface about dataSnapshots that entered, exited, or moved within the query.
 */

using System;
using Plugin.CloudFirestore;

namespace GeoFire
{
    internal sealed class EventListenerBridge : IGeoQueryDataEventListener 
    {
        private readonly IGeoQueryEventListener _listener;

        public EventListenerBridge(IGeoQueryEventListener listener) {
            _listener = listener;
        }
        
        public void OnDocumentEntered(IDocumentSnapshot dataSnapshot, GeoPoint location) 
        {
            _listener.OnKeyEntered(dataSnapshot.Id, location);
        }
        
        public void OnDocumentExited(IDocumentSnapshot dataSnapshot) 
        {
            _listener.OnKeyExited(dataSnapshot.Id);
        }
        
        public void OnDocumentMoved(IDocumentSnapshot dataSnapshot, GeoPoint location) 
        {
            _listener.OnKeyMoved(dataSnapshot.Id, location);
        }
        
        public void OnDocumentChanged(IDocumentSnapshot dataSnapshot, GeoPoint location) 
        {
            // No-op.
        }
        
        public void OnGeoQueryReady() 
        {
            _listener.OnGeoQueryReady();
        }

        public void OnGeoQueryError(Exception e) 
        {
            _listener.OnGeoQueryError(e);
        }

        private bool Equals(EventListenerBridge other)
        {
            return _listener.Equals(other._listener);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is EventListenerBridge other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _listener.GetHashCode();
        }
    }
}
