// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Authentication;
using UnityEngine;

namespace Firebase.RealtimeStorage
{
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
        /// <param name="streamUpdates">Should the <see cref="DatabaseEndpoint{T}"/> stream changes from the client? (Defaults to true)</param>
        public DatabaseEndpoint(FirebaseRealtimeDatabaseClient client, string endpoint, bool streamUpdates = true)
        {
            this.client = client;
            EndPoint = endpoint;
            Task.Run(GetDataSnapshotAsync);

            if (streamUpdates)
            {
                Task.Run(OnDatabaseUpdateAsync);
            }

            async Task OnDatabaseUpdateAsync()
            {
                await StreamChangesAsync(OnDatabaseEndpointValueChanged, CancellationToken.None);
            }
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
            => await SetDataSnapshotAsync(JsonUtility.ToJson(newValue));

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

        /// <summary>
        /// Stream data snapshots from the <see cref="EndPoint"/>.
        /// </summary>
        /// <param name="resultHandler">The response handler to subscribe to when streaming events are raised.</param>
        /// <param name="cancellationToken">Cancellation token to use when streaming needs to stop.</param>
        public async Task StreamChangesAsync(Action<FirebaseEventType, T> resultHandler, CancellationToken cancellationToken)
        {
            await client.StreamDataSnapshotChangesAsync(EndPoint, (eventType, stringResponse) =>
            {
                Debug.Log($"[{eventType}] {stringResponse.Path}\n{stringResponse.Data}");
                resultHandler(eventType, default);
            }, cancellationToken);
        }

        private void OnDatabaseEndpointValueChanged(FirebaseEventType eventType, T updatedValue)
        {
            switch (eventType)
            {
                case FirebaseEventType.None:
                    break;
                case FirebaseEventType.Put:
                    break;
                case FirebaseEventType.Patch:
                    break;
                case FirebaseEventType.Cancel:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
            }
        }

        /// <inheritdoc />
        public override string ToString() => jsonValue;
    }
}
