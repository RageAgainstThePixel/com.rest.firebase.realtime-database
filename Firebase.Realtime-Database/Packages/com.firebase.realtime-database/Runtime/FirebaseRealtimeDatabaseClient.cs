// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Firebase.RealtimeStorage
{
    public class FirebaseRealtimeDatabaseClient
    {
        /// <summary>
        /// Creates a new instance of a <see cref="FirebaseRealtimeDatabaseClient"/>.
        /// </summary>
        /// <param name="authenticationClient">The <see cref="FirebaseAuthenticationClient"/> to use when making authenticated calls to the database.</param>
        /// <param name="databaseEndpoint">Optional, override database endpoint to use. (defaults to 'https://project-id-default-rtdb.firebaseio.com/'.</param>
        public FirebaseRealtimeDatabaseClient(FirebaseAuthenticationClient authenticationClient, string databaseEndpoint = null)
        {
            AuthenticationClient = authenticationClient;
            DatabaseEndpoint = databaseEndpoint ?? $"https://{authenticationClient.Configuration.ProjectId}-default-rtdb.firebaseio.com/";
            HttpClient = new HttpClient();
        }

        /// <summary>
        /// The root database endpoint.
        /// </summary>
        public string DatabaseEndpoint { get; }

        private FirebaseAuthenticationClient AuthenticationClient { get; }

        private HttpClient HttpClient { get; }

        /// <summary>
        /// Gets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to get the data from.</param>
        /// <returns>A Json string representation of the data at the specified endpoint.</returns>
        public async Task<string> GetDataSnapshotAsync(string endpoint)
        {
            var idToken = await AuthenticationClient.User.GetIdTokenAsync().ConfigureAwait(false);
            var response = await HttpClient.GetAsync($"{DatabaseEndpoint}{endpoint}.json?auth={idToken}").ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the data to.</param>
        /// <param name="json">The Json string representation of the data to set at the specified endpoint.</param>
        /// <returns>The server response.</returns>
        public async Task<string> SetDataSnapshotAsync(string endpoint, string json)
        {
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            var idToken = await AuthenticationClient.User.GetIdTokenAsync().ConfigureAwait(false);
            var response = await HttpClient.PutAsync($"{DatabaseEndpoint}{endpoint}.json?auth={idToken}", content).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Deletes data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to delete the data at.</param>
        public async Task DeleteDataSnapshotAsync(string endpoint)
        {
            var idToken = await AuthenticationClient.User.GetIdTokenAsync().ConfigureAwait(false);
            await HttpClient.DeleteAsync($"{DatabaseEndpoint}{endpoint}.json?auth={idToken}").ConfigureAwait(false);
        }
    }
}
