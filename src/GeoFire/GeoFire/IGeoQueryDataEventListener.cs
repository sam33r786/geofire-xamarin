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
 * GeoQuery notifies listeners with this interface about documents that entered, exited, or moved within the query.
 */

using System;
using Plugin.CloudFirestore;

namespace GeoFire
{
   public interface IGeoQueryDataEventListener
   {

      /**
         * Called if a document entered the search area of the GeoQuery. This method is called for every document currently in the
         * search area at the time of adding the listener.
         *
         * This method is once per datasnapshot, and is only called again if onDataExited was called in the meantime.
         *
         * @param document The associated document that entered the search area
         * @param location The location for this document as a GeoLocation object
         */
      void OnDocumentEntered(IDocumentSnapshot document, GeoPoint location);

      /**
         * Called if a datasnapshot exited the search area of the GeoQuery. This is method is only called if onDataEntered was called
         * for the datasnapshot.
         *
         * @param document The associated document that exited the search area
         */
      void OnDocumentExited(IDocumentSnapshot document);

      /**
         * Called if a document moved within the search area.
         *
         * This method can be called multiple times.
         *
         * @param document The associated document that moved within the search area
         * @param location The location for this document as a GeoLocation object
         */
      void OnDocumentMoved(IDocumentSnapshot document, GeoPoint location);

      /**
         * Called if a document changed within the search area.
         *
         * An onDataMoved() is always followed by onDataChanged() but it is be possible to see
         * onDataChanged() without an preceding onDataMoved().
         *
         * This method can be called multiple times for a single location change, due to the way
         * the Realtime Database handles floating point numbers.
         *
         * Note: this method is not related to ValueEventListener#onDataChange(DataSnapshot).
         *
         * @param document The associated document that moved within the search area
         * @param location The location for this document as a GeoLocation object
         */
      void OnDocumentChanged(IDocumentSnapshot document, GeoPoint location);

      /**
         * Called once all initial GeoFire data has been loaded and the relevant events have been fired for this query.
         * Every time the query criteria is updated, this observer will be called after the updated query has fired the
         * appropriate document entered or document exited events.
         */
      void OnGeoQueryReady();

      /**
         * Called in case an error occurred while retrieving locations for a query, e.g. violating security rules.
         * @param error The error that occurred while retrieving the query
         */
      void OnGeoQueryError(Exception e);

   }
}
