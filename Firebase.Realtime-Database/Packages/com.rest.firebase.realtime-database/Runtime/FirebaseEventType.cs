// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Firebase.RealtimeDatabase
{
    public enum FirebaseEventType
    {
        None,
        /// <summary>
        /// The JSON-encoded data will be an object with two keys: path and data
        /// The path points to a location relative to the request URL
        ///    The client should replace all of the data at that location in its cache with the data given in the message
        /// </summary>
        Put,
        /// <summary>
        /// The JSON-encoded data will be an object with two keys: path and data
        ///    The path points to a location relative to the request URL
        /// For each key in the data, the client should replace the corresponding key in its cache with the data for that key in the message
        /// </summary>
        Patch,
        /// <summary>
        /// The data for this event is null
        /// This event will be sent if the Firebase Realtime Database Rules cause a read at the requested location to no longer be allowed
        /// </summary>
        Cancel,
    }
}
