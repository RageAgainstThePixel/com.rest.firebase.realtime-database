// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
        /// Stream data updates from the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to stream data snapshots from.</param>
        /// <param name="responseHandler">The response handler to subscribe to when streaming events are raised.</param>
        /// <param name="cancellationToken">Cancellation token to use when streaming needs to stop.</param>
        public async Task StreamDataSnapshotChangesAsync(string endpoint, Action<string> responseHandler, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, $"{DatabaseEndpoint}{endpoint}"))
            {
                var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var reader = new StreamReader(stream);
                    string line;

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (line.StartsWith("data: "))
                        {
                            line = line.Substring(5);
                            // equivalent to:
                            // line = line["data: ".Length..];
                        }

                        if (line == "[DONE]")
                        {
                            return;
                        }

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            responseHandler(line.Trim());
                        }
                    }
                }
                else
                {
                    throw new HttpRequestException($"{nameof(StreamDataSnapshotChangesAsync)}::{endpoint} Failed! HTTP status code: {response.StatusCode}.");
                }
            }
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

    /// <summary>
    /// Represents a database endpoint.
    /// </summary>
    /// <typeparam name="T">The value type expected at the endpoint.</typeparam>
    public class DatabaseEndpoint<T>
    {
        /// <summary>
        /// Creates a new <see cref="DatabaseEndpoint{T}"/> instance for the specified endpoint.
        /// </summary>
        /// <param name="client">The <see cref="FirebaseAuthenticationClient"/> to use when making requests to the <see cref="EndPoint"/>.</param>
        /// <param name="endpoint">The string uri of the <see cref="EndPoint"/> relative to the <see cref="FirebaseRealtimeDatabaseClient.DatabaseEndpoint"/>.</param>
        public DatabaseEndpoint(FirebaseRealtimeDatabaseClient client, string endpoint)
        {
            this.client = client;
            EndPoint = endpoint;
            Task.Run(GetDataSnapshotAsync);
        }

        private readonly FirebaseRealtimeDatabaseClient client;

        /// <summary>
        /// The database endpoint.
        /// </summary>
        public string EndPoint { get; }

        /// <summary>
        /// Event raised when <see cref="Value"/> is changed or updated.
        /// </summary>
        public event Action<T> OnValueChanged;

        private string jsonValue;

        private T value;

        /// <summary>
        /// The current value of the <see cref="EndPoint"/>.
        /// </summary>
        public T Value
        {
            get => value;
            set
            {
                var newValueJson = JsonUtility.ToJson(value);

                if (newValueJson != jsonValue)
                {
                    jsonValue = newValueJson;
                    this.value = value;
                    OnValueChanged?.Invoke(value);
                    Task.Run(() => SetDataSnapshotAsync(jsonValue));
                }
            }
        }

        /// <summary>
        /// Manually get the current value of the <see cref="EndPoint"/>.
        /// </summary>
        /// <returns>The current value of the <see cref="EndPoint"/>.</returns>
        public async Task<T> GetDataSnapshotAsync()
        {
            var result = await client.GetDataSnapshotAsync(EndPoint);

            if (result != jsonValue)
            {
                jsonValue = result;
                value = JsonUtility.FromJson<T>(jsonValue);
                OnValueChanged?.Invoke(value);
            }

            return Value;
        }

        /// <summary>
        /// Manually set the <see cref="EndPoint"/> to the <see cref="newValue"/> provided.
        /// </summary>
        /// <param name="newValue">The <see cref="newValue"/> to set at the <see cref="EndPoint"/>.</param>
        public async Task SetDataSnapshotAsync(T newValue)
        {
            await SetDataSnapshotAsync(JsonUtility.ToJson(newValue));
        }

        private async Task SetDataSnapshotAsync(string newValue)
        {
            var result = await client.SetDataSnapshotAsync(EndPoint, newValue);
            Debug.Assert(result == newValue);
        }

        /// <summary>
        /// Deletes all data at the <see cref="EndPoint"/>.
        /// </summary>
        public async Task DeleteSnapshotAsync()
        {
            await client.DeleteDataSnapshotAsync(jsonValue);
            await GetDataSnapshotAsync();
        }

        private async Task StreamChangesAsync(Action<T> resultHandler, CancellationToken token)
        {
            await client.StreamDataSnapshotChangesAsync(EndPoint, stringResponse =>
            {
                resultHandler(JsonUtility.FromJson<T>(stringResponse));
            }, token);
        }

        /// <inheritdoc />
        public override string ToString() => jsonValue;
    }
}
