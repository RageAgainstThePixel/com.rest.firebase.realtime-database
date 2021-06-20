// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Firebase.RealtimeStorage
{
    /// <summary>
    /// Represents a database endpoint.
    /// </summary>
    /// <typeparam name="T">The value type expected at the endpoint.</typeparam>
    public class DatabaseEndpoint<T> : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="DatabaseEndpoint{T}"/> instance for the specified endpoint.
        /// </summary>
        /// <param name="client">The <see cref="FirebaseAuthenticationClient"/> to use when making requests to the <see cref="EndPoint"/>.</param>
        /// <param name="endpoint">The string uri of the <see cref="EndPoint"/> relative to the <see cref="FirebaseRealtimeDatabaseClient.DatabaseEndpoint"/>.</param>
        /// <param name="streamUpdates">Should the <see cref="DatabaseEndpoint{T}"/> stream changes from the client and raise <see cref="OnValueChanged"/> events? (Defaults to true)</param>
        /// <remarks>
        /// Don't forget to call <see cref="Dispose()"/> if streaming value updates.
        /// </remarks>
        public DatabaseEndpoint(FirebaseRealtimeDatabaseClient client, string endpoint, bool streamUpdates = true)
        {
            this.client = client;
            EndPoint = endpoint;
            SyncDataSnapshot();

            if (streamUpdates)
            {
                cancellationTokenSource = new CancellationTokenSource();
                StartStreamingEndpoint(cancellationTokenSource.Token);
            }
        }

        ~DatabaseEndpoint()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource?.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly FirebaseRealtimeDatabaseClient client;
        private readonly CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// The database endpoint.
        /// </summary>
        public string EndPoint { get; }

        /// <summary>
        /// Event raised when <see cref="Value"/> is changed or updated.
        /// </summary>
        public event Action<T> OnValueChanged;

        private string json = null;

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

                if (json != newValueJson)
                {
                    json = newValueJson;
                    this.value = value;
                    OnValueChanged?.Invoke(value);
                    SetDataSnapshot(json);
                }
            }
        }

        private async void SyncDataSnapshot()
            => await GetDataSnapshotAsync();

        /// <summary>
        /// Manually get the current value of the <see cref="EndPoint"/>.
        /// </summary>
        /// <returns>The current value of the <see cref="EndPoint"/>.</returns>
        public async Task<T> GetDataSnapshotAsync()
        {
            var snapshot = await client.GetDataSnapshotAsync(EndPoint);

            if (json != snapshot)
            {
                json = snapshot;
                value = JsonUtility.FromJson<T>(json);
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
            var newValueJson = JsonUtility.ToJson(newValue);

            if (json != newValueJson)
            {
                json = newValueJson;
                value = newValue;
                OnValueChanged?.Invoke(value);
                await SetDataSnapshotAsync(newValueJson);
            }
        }

        private async void SetDataSnapshot(string newValue)
            => await SetDataSnapshotAsync(newValue);

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
            json = null;
            value = default;
            await client.DeleteDataSnapshotAsync(json);
        }

        private async void StartStreamingEndpoint(CancellationToken cancellationToken)
            => await client.StreamDataSnapshotChangesAsync(EndPoint, OnDatabaseEndpointValueChanged, cancellationToken);

        /// <summary>
        /// Stream data snapshots from the <see cref="EndPoint"/>.
        /// </summary>
        /// <param name="resultHandler">The response handler to subscribe to when streaming events are received.</param>
        /// <remarks>
        /// This task does not return until the database event is cancelled or the <see cref="DatabaseEndpoint{T}"/> has been disposed.
        /// </remarks>
        public async Task StreamChangesAsync(Action<FirebaseEventType, T> resultHandler)
        {
            await client.StreamDataSnapshotChangesAsync(EndPoint, ResponseHandler, cancellationTokenSource.Token);

            void ResponseHandler(FirebaseEventType eventType, StreamedSnapShotResponse snapshot)
            {
                OnDatabaseEndpointValueChanged(eventType, snapshot);
                resultHandler(eventType, value);
            }
        }

        private void OnDatabaseEndpointValueChanged(FirebaseEventType eventType, StreamedSnapShotResponse snapshot)
        {
            switch (eventType)
            {
                case FirebaseEventType.None:
                    break;
                case FirebaseEventType.Put:
                case FirebaseEventType.Patch:
                    if (json != snapshot?.Data)
                    {
                        json = snapshot?.Data;
                        value = JsonUtility.FromJson<T>(json);
                        OnValueChanged?.Invoke(value);
                    }
                    break;
                case FirebaseEventType.Cancel:
                    json = null;
                    value = default;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
            }
        }

        /// <inheritdoc />
        public override string ToString() => EndPoint ?? "null";
    }
}
