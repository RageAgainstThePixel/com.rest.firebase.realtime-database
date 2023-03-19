// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using Firebase.RealtimeDatabase.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using HttpClientExtensions = Firebase.RealtimeDatabase.Extensions.HttpClientExtensions;

namespace Firebase.RealtimeDatabase
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
            HttpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true
            });
        }

        /// <summary>
        /// The root database endpoint.
        /// </summary>
        public string DatabaseEndpoint { get; }

        private FirebaseAuthenticationClient AuthenticationClient { get; }

        private HttpClient HttpClient { get; }

        private async Task<string> GetAuthenticatedEndpointAsync(string endpoint)
        {
            var idToken = await AuthenticationClient.User.GetIdTokenAsync().ConfigureAwait(false);
            return $"{DatabaseEndpoint}{endpoint}.json?auth={idToken}";
        }

        [Obsolete("Use GetSnapshotAsync instead")]
        public async Task<string> GetDataSnapshotAsync(string endpoint, CancellationToken cancellationToken = default)
            => await GetSnapshotAsync(endpoint, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Gets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to get the data from.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/></param>
        /// <returns>A Json string representation of the data at the specified endpoint.</returns>
        public async Task<string> GetSnapshotAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            var request = await GetAuthenticatedEndpointAsync(endpoint).ConfigureAwait(false);
            using var response = await HttpClient.GetAsync(request, cancellationToken).ConfigureAwait(false);
            var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (message.Contains("error"))
            {
                throw new FirebaseRealtimeDatabaseException(message);
            }

            return ValidateMessageData(message);
        }

        [Obsolete("Use PutSnapshotAsync instead")]
        public async Task<string> SetDataSnapshotAsync(string endpoint, string json, CancellationToken cancellationToken = default)
            => await PutSnapshotAsync(endpoint, json, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Sets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the data to.</param>
        /// <param name="json">The Json string representation of the data to set at the specified endpoint.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/></param>
        /// <returns>The server response.</returns>
        /// <remarks>This will completely overwrite any data already set at the endpoint.</remarks>
        public async Task<string> PutSnapshotAsync(string endpoint, string json, CancellationToken cancellationToken = default)
        {
            var request = await GetAuthenticatedEndpointAsync(endpoint).ConfigureAwait(false);
            using var response = await HttpClient.PutAsync(request, json.ToJsonStringContent(), cancellationToken).ConfigureAwait(false);
            var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (message.Contains("error"))
            {
                throw new FirebaseRealtimeDatabaseException(message);
            }

            return ValidateMessageData(message);
        }

        /// <summary>
        /// Posts data at the specified endpoint.<br/>
        /// This is equivalent to the JavaScript push() function.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the data to.</param>
        /// <param name="json">The Json string representation of the data to set at the specified endpoint.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/></param>
        /// <returns>The server response.</returns>
        public async Task<string> PostSnapShotAsync(string endpoint, string json, CancellationToken cancellationToken = default)
        {
            var request = await GetAuthenticatedEndpointAsync(endpoint).ConfigureAwait(false);
            using var response = await HttpClient.PostAsync(request, json.ToJsonStringContent(), cancellationToken).ConfigureAwait(false);
            var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (message.Contains("error"))
            {
                throw new FirebaseRealtimeDatabaseException(message);
            }

            return ValidateMessageData(message);
        }

        [Obsolete("Use PatchSnapshotAsync instead")]
        public async Task<string> UpdateDataSnapshotAsync(string endpoint, string json, CancellationToken cancellationToken = default)
            => await PatchSnapshotAsync(endpoint, json, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Patch data at the specified endpoint without overwriting existing data.
        /// Named children in the data being written with Patch are overwritten, but omitted children are not deleted.<br/>
        /// This is equivalent to the JavaScript update() function.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the data to.</param>
        /// <param name="json">The Json string representation of the data to set at the specified endpoint.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/></param>
        /// <returns>The server response.</returns>
        public async Task<string> PatchSnapshotAsync(string endpoint, string json, CancellationToken cancellationToken = default)
        {
            var request = await GetAuthenticatedEndpointAsync(endpoint).ConfigureAwait(false);
            using var response = await HttpClientExtensions.PatchAsync(HttpClient, request, json.ToJsonStringContent(), cancellationToken).ConfigureAwait(false);
            var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (message.Contains("error"))
            {
                throw new FirebaseRealtimeDatabaseException(message);
            }

            return ValidateMessageData(message);
        }

        /// <summary>
        /// Stream data snapshots from the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to stream data snapshots from.</param>
        /// <param name="responseHandler">The response handler to subscribe to when streaming events are raised.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/> to use when streaming needs to be stopped.</param>
        public async Task StreamDataSnapshotChangesAsync(string endpoint, Action<FirebaseEventType, StreamedSnapShotResponse> responseHandler, CancellationToken cancellationToken = default)
        {
            var eventType = FirebaseEventType.None;
            using var response = await ListenAsync(endpoint, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{nameof(StreamDataSnapshotChangesAsync)}::{endpoint} Failed! HTTP status code: {response.StatusCode}.");
            }

            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));

            while (!cancellationToken.IsCancellationRequested)
            {
                string line;

                if ((line = await reader.ReadLineAsync().ConfigureAwait(false)) == null)
                {
                    Debug.LogWarning($"Stream for \"{endpoint}\" ended unexpectedly!");
                    break;
                }

                if (line.StartsWith("event: "))
                {
                    line = line[6..].Trim();

                    eventType = line switch
                    {
                        // The JSON-encoded data will be an object with two keys: path and data
                        // The path points to a location relative to the request URL
                        //    The client should replace all of the data at that location in its cache with the data given in the message
                        "put" => FirebaseEventType.Put,
                        // The JSON-encoded data will be an object with two keys: path and data
                        //    The path points to a location relative to the request URL
                        // For each key in the data, the client should replace the corresponding key in its cache with the data for that key in the message
                        "patch" => FirebaseEventType.Patch,
                        // The data for this event is null, no action is required
                        "keep-alive" => FirebaseEventType.None,
                        // The data for this event is null
                        // This event will be sent if the Firebase Realtime Database Rules cause a read at the requested location to no longer be allowed
                        "cancel" => FirebaseEventType.Cancel,
                        // The data for this event is a string indicating that a the credential has expired
                        // This event will be sent when the supplied auth parameter is no longer valid
                        "auth_revoked" => throw new UnauthorizedAccessException("The credentials have expired!"),
                        _ => eventType
                    };
                }

                if (line.StartsWith("data: "))
                {
                    line = line[5..].Trim();

                    var json = ValidateMessageData(line);

                    if (eventType == FirebaseEventType.None ||
                        string.IsNullOrWhiteSpace(json))
                    {
                        continue;
                    }

                    string data;
                    var jsonResult = JObject.Parse(json);
                    data = jsonResult[nameof(data)]?.Type == JTokenType.Null
                        ? null
                        : jsonResult[nameof(data)]?.ToString(Formatting.None);
                    data = data?.Replace("\n", string.Empty);
                    var snapShotResponse = JsonUtility.FromJson<StreamedSnapShotResponse>(json) ?? new StreamedSnapShotResponse();
                    snapShotResponse.Data = data;
                    cancellationToken.ThrowIfCancellationRequested();
                    responseHandler(eventType, snapShotResponse);
                }
            }

            response.RequestMessage.Dispose();
        }

        private async Task<HttpResponseMessage> ListenAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            // Create HTTP Client which will allow auto redirect as required by Firebase
            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };

            var httpClient = new HttpClient(httpClientHandler, true);
            httpClient.BaseAddress = new Uri(DatabaseEndpoint);
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            var requestUri = new Uri(await GetAuthenticatedEndpointAsync(endpoint).ConfigureAwait(false));
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return response;
        }

        [Obsolete("Use DeleteSnapshotAsync instead")]
        public async Task DeleteDataSnapshotAsync(string endpoint, CancellationToken cancellationToken = default)
            => await DeleteSnapshotAsync(endpoint, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Deletes data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to delete the data at.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/></param>
        public async Task DeleteSnapshotAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            var idToken = await AuthenticationClient.User.GetIdTokenAsync().ConfigureAwait(false);
            var response = await HttpClient.DeleteAsync($"{DatabaseEndpoint}{endpoint}.json?auth={idToken}", cancellationToken).ConfigureAwait(false);
            var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (message.Contains("error"))
            {
                throw new FirebaseRealtimeDatabaseException(message);
            }
        }

        private static string ValidateMessageData(string message)
            => !string.IsNullOrWhiteSpace(message) && message != "null"
                ? message.Trim()
                : null;
    }
}
