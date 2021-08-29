// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Firebase.RealtimeDatabase
{
    public class FirebaseRealtimeDatabaseException : Exception
    {
        public FirebaseRealtimeDatabaseException(string message, Exception innerException = null)
            : base(message.Replace("\n", string.Empty), innerException)
        {
        }
    }
}
