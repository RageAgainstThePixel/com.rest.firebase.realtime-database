// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Firebase.RealtimeDatabase
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
        /// <param name="jsonSerializerSettings">The json serializer settings to use for <see cref="T"/>.</param>
        /// <remarks>
        /// Don't forget to call <see cref="Dispose()"/> if streaming value updates.
        /// </remarks>
        public DatabaseEndpoint(FirebaseRealtimeDatabaseClient client, string endpoint = null, bool streamUpdates = true, JsonSerializerSettings jsonSerializerSettings = null)
        {
            if (typeof(T).IsAbstract)
            {
                throw new InvalidConstraintException($"{nameof(DatabaseEndpoint<T>)} cannot use an abstract generic parameter \"{typeof(T).Name}\".");
            }

            isCollection = typeof(T).IsCollection();

            this.client = client;
            EndPoint = endpoint ?? $"{(isCollection ? $"{typeof(T).GetGenericArguments().FirstOrDefault().Name.ToLower()}s" : typeof(T).Name.ToLower())}";

            Debug.Log($"Created {nameof(endpoint)}: {EndPoint}");
            SerializerSettings = jsonSerializerSettings;
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

        private readonly bool isCollection;
        private readonly FirebaseRealtimeDatabaseClient client;
        private readonly CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// The database endpoint.
        /// </summary>
        public string EndPoint { get; }

        /// <summary>
        /// Json serializer settings to use.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; set; }

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
                var newValueJson = JsonConvert.SerializeObject(value, SerializerSettings);

                if (json != newValueJson)
                {
                    json = newValueJson;
                    this.value = value;
                    OnValueChanged?.Invoke(value);
                    SetDataSnapshot(json);
                }
            }
        }

        public static implicit operator T(DatabaseEndpoint<T> endpoint) => endpoint.Value;

        #region Equality

        /// <inheritdoc />
        public override bool Equals(object @object)
        {
            switch (@object)
            {
                case DatabaseEndpoint<T> otherEndpoint:
                    return Equals(otherEndpoint);
                case T otherValue:
                    return Equals(otherValue);
                default:
                    return false;
            }
        }

        public bool Equals(DatabaseEndpoint<T> other)
            => Equals(other.value) && EndPoint == other.EndPoint;

        public bool Equals(T other) => EqualityComparer<T>.Default.Equals(value, other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once NonReadonlyMemberInGetHashCode
                return (EqualityComparer<T>.Default.GetHashCode(value) * 397) ^ (EndPoint != null ? EndPoint.GetHashCode() : 0);
            }
        }

        public static bool operator ==(DatabaseEndpoint<T> left, T right)
            => left != null && left.Equals(right);

        public static bool operator !=(DatabaseEndpoint<T> left, T right)
            => !(left == right);

        public static bool operator ==(T left, DatabaseEndpoint<T> right)
            => right != null && right.Equals(left);

        public static bool operator !=(T left, DatabaseEndpoint<T> right)
            => !(left == right);

        public static bool operator ==(DatabaseEndpoint<T> left, DatabaseEndpoint<T> right)
            => left != null && left.Equals(right);

        public static bool operator !=(DatabaseEndpoint<T> left, DatabaseEndpoint<T> right)
            => !(left == right);

        #endregion Equality

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
                value = !string.IsNullOrWhiteSpace(json)
                    ? JsonConvert.DeserializeObject<T>(json, SerializerSettings)
                    : default;
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
            var newValueJson = JsonConvert.SerializeObject(newValue, SerializerSettings);

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
        {
            try
            {
                await client.StreamDataSnapshotChangesAsync(EndPoint, OnDatabaseEndpointValueChanged, cancellationToken);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

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
                        value = !string.IsNullOrWhiteSpace(json)
                            ? JsonConvert.DeserializeObject<T>(json, SerializerSettings)
                            : default;
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
        public override string ToString() => $"{EndPoint}/{json}" ?? "null";
    }
}
