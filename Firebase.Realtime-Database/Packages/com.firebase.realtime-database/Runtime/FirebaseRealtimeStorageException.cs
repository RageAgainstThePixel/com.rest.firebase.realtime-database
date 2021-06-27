// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Firebase.RealtimeDatabase
{
    public class FirebaseRealtimeDatabaseException : Exception
    {
        public FirebaseRealtimeDatabaseException(string message)
            : base(message.Replace("\n", string.Empty))
        {
        }
    }
}
