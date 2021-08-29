// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using Firebase.RealtimeDatabase.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
            var idToken = await AuthenticationClient.User.GetIdTokenAsync();
            return $"{DatabaseEndpoint}{endpoint}.json?auth={idToken}";
        }

        /// <summary>
        /// Gets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to get the data from.</param>
        /// <returns>A Json string representation of the data at the specified endpoint.</returns>
        public async Task<string> GetDataSnapshotAsync(string endpoint)
            => await GetDataSnapshotAsync(endpoint, CancellationToken.None);

        /// <summary>
        /// Gets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to get the data from.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A Json string representation of the data at the specified endpoint.</returns>
        public async Task<string> GetDataSnapshotAsync(string endpoint, CancellationToken cancellationToken)
        {
            var request = await GetAuthenticatedEndpointAsync(endpoint);

            using (var response = await HttpClient.GetAsync(request, cancellationToken))
            {
                var message = await response.Content.ReadAsStringAsync();

                if (message.Contains("error"))
                {
                    throw new FirebaseRealtimeDatabaseException(message);
                }

                return ValidateMessageData(message);
            }
        }

        /// <summary>
        /// Sets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the data to.</param>
        /// <param name="json">The Json string representation of the data to set at the specified endpoint.</param>
        /// <returns>The server response.</returns>
        /// <remarks>This will completely overwrite any data already set at the endpoint.</remarks>
        public async Task<string> SetDataSnapshotAsync(string endpoint, string json)
            => await SetDataSnapshotAsync(endpoint, json, CancellationToken.None);

        /// <summary>
        /// Sets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the data to.</param>
        /// <param name="json">The Json string representation of the data to set at the specified endpoint.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The server response.</returns>
        /// <remarks>This will completely overwrite any data already set at the endpoint.</remarks>
        public async Task<string> SetDataSnapshotAsync(string endpoint, string json, CancellationToken cancellationToken)
        {
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = await GetAuthenticatedEndpointAsync(endpoint);

            using (var response = await HttpClient.PutAsync(request, content, cancellationToken))
            {
                var message = await response.Content.ReadAsStringAsync();

                if (message.Contains("error"))
                {
                    throw new FirebaseRealtimeDatabaseException(message);
                }

                return ValidateMessageData(message);
            }
        }

        /// <summary>
        /// Updates or sets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the data to.</param>
        /// <param name="json">The Json string representation of the data to set at the specified endpoint.</param>
        /// <returns>The server response.</returns>
        public async Task<string> UpdateDataSnapshotAsync(string endpoint, string json)
            => await UpdateDataSnapshotAsync(endpoint, json, CancellationToken.None);

        /// <summary>
        /// Updates or sets data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to set the data to.</param>
        /// <param name="json">The Json string representation of the data to set at the specified endpoint.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The server response.</returns>
        public async Task<string> UpdateDataSnapshotAsync(string endpoint, string json, CancellationToken cancellationToken)
        {
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = await GetAuthenticatedEndpointAsync(endpoint);

            using (var response = await HttpClient.PatchAsync(request, content, cancellationToken))
            {
                var message = await response.Content.ReadAsStringAsync();

                if (message.Contains("error"))
                {
                    throw new FirebaseRealtimeDatabaseException(message);
                }

                return ValidateMessageData(message);
            }
        }

        /// <summary>
        /// Stream data snapshots from the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to stream data snapshots from.</param>
        /// <param name="responseHandler">The response handler to subscribe to when streaming events are raised.</param>
        /// <param name="cancellationToken">Cancellation token to use when streaming needs to stop.</param>
        public async Task StreamDataSnapshotChangesAsync(string endpoint, Action<FirebaseEventType, StreamedSnapShotResponse> responseHandler, CancellationToken cancellationToken)
        {
            var eventType = FirebaseEventType.None;
            var request = new HttpRequestMessage(HttpMethod.Get, await GetAuthenticatedEndpointAsync(endpoint));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using (var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"{nameof(StreamDataSnapshotChangesAsync)}::{endpoint} Failed! HTTP status code: {response.StatusCode}.");
                }

                using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        string line;

                        if ((line = await reader.ReadLineAsync()) == null)
                        {
                            Debug.LogWarning($"Stream for \"{endpoint}\" ended unexpectedly!");
                            break;
                        }

                        if (line.StartsWith("event: "))
                        {
                            line = line.Substring(6).Trim();

                            switch (line)
                            {
                                case "put":
                                    // The JSON-encoded data will be an object with two keys: path and data
                                    // The path points to a location relative to the request URL
                                    //    The client should replace all of the data at that location in its cache with the data given in the message
                                    eventType = FirebaseEventType.Put;
                                    break;
                                case "patch":
                                    // The JSON-encoded data will be an object with two keys: path and data
                                    //    The path points to a location relative to the request URL
                                    // For each key in the data, the client should replace the corresponding key in its cache with the data for that key in the message
                                    eventType = FirebaseEventType.Patch;
                                    break;
                                case "keep-alive":
                                    // The data for this event is null, no action is required
                                    eventType = FirebaseEventType.None;
                                    break;
                                case "cancel":
                                    // The data for this event is null
                                    // This event will be sent if the Firebase Realtime Database Rules cause a read at the requested location to no longer be allowed
                                    eventType = FirebaseEventType.Cancel;
                                    break;
                                case "auth_revoked":
                                    // The data for this event is a string indicating that a the credential has expired
                                    // This event will be sent when the supplied auth parameter is no longer valid
                                    throw new UnauthorizedAccessException("The credentials have expired!");
                            }
                        }

                        if (line.StartsWith("data: "))
                        {
                            line = line.Substring(5).Trim();

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

                            var snapShotResponse = JsonUtility.FromJson<StreamedSnapShotResponse>(json);

                            if (snapShotResponse == null)
                            {
                                snapShotResponse = new StreamedSnapShotResponse();
                            }

                            snapShotResponse.Data = data;
                            responseHandler(eventType, snapShotResponse);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to delete the data at.</param>
        public async Task DeleteDataSnapshotAsync(string endpoint)
            => await DeleteDataSnapshotAsync(endpoint, CancellationToken.None);

        /// <summary>
        /// Deletes data at the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to delete the data at.</param>
        /// <param name="cancellationToken"></param>
        public async Task DeleteDataSnapshotAsync(string endpoint, CancellationToken cancellationToken)
        {
            var idToken = await AuthenticationClient.User.GetIdTokenAsync();
            var response = await HttpClient.DeleteAsync($"{DatabaseEndpoint}{endpoint}.json?auth={idToken}", cancellationToken);
            response.Dispose();
        }

        private static string ValidateMessageData(string message)
            => !string.IsNullOrWhiteSpace(message) && message != "null"
                ? message.Trim()
                : null;
    }
}
