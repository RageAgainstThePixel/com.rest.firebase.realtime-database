// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;

namespace Firebase.RealtimeStorage
{
    public class FirebaseRealtimeStorageClient
    {
        /// <summary>
        /// Creates a new instance of a <see cref="FirebaseRealtimeStorageClient"/>.
        /// </summary>
        /// <param name="authenticationClient">The <see cref="FirebaseAuthenticationClient"/> to use when making authenticated calls to the database.</param>
        /// <param name="databaseEndpoint">Optional, override database endpoint to use. (defaults to 'https://project-id-default-rtdb.firebaseio.com/'.</param>
        public FirebaseRealtimeStorageClient(FirebaseAuthenticationClient authenticationClient, string databaseEndpoint = null)
        {
            AuthenticationClient = authenticationClient;
            DatabaseEndpoint = databaseEndpoint ?? $"https://{authenticationClient.Configuration.ProjectId}-default-rtdb.firebaseio.com/";
        }

        public string DatabaseEndpoint { get; }

        internal FirebaseAuthenticationClient AuthenticationClient { get; }
    }
}
