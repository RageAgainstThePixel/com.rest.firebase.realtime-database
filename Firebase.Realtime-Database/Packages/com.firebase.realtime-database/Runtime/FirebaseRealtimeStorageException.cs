// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Firebase.RealtimeStorage
{
    public class FirebaseRealtimeStorageException : Exception
    {
        public FirebaseRealtimeStorageException(string message)
            : base(message.Replace("\n", string.Empty))
        {
        }
    }
}
