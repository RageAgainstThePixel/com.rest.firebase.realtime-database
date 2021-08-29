// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebase.RealtimeDatabase.Extensions;
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
        /// <param name="value">The value to set on the database.</param>
        /// <param name="endpoint">The string uri of the <see cref="EndPoint"/> relative to the <see cref="FirebaseRealtimeDatabaseClient.DatabaseEndpoint"/>.</param>
        /// <param name="jsonSerializerSettings">The json serializer settings to use for <see cref="T"/>.</param>
        /// <remarks>
        /// Don't forget to call <see cref="Dispose()"/> if streaming value updates.
        /// </remarks>
        public DatabaseEndpoint(FirebaseRealtimeDatabaseClient client, T value, string endpoint = null, JsonSerializerSettings jsonSerializerSettings = null)
            : this(client, endpoint, false, jsonSerializerSettings)
        {
            Value = value;
        }

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

            var isCollection = typeof(T).IsCollection();

            this.client = client;
            EndPoint = endpoint ?? $"{(isCollection ? $"{typeof(T).GetGenericArguments().FirstOrDefault()?.Name.ToLower()}s" : typeof(T).Name.ToLower())}";

            Debug.Log($"Created {nameof(endpoint)}: {EndPoint}");
            SerializerSettings = jsonSerializerSettings;
            cancellationTokenSource = new CancellationTokenSource();

            if (streamUpdates)
            {
                streamingThread = new Thread(() => StartStreamingEndpoint(cancellationTokenSource.Token))
                {
                    IsBackground = true
                };

                streamingThread.Start();
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
                streamingThread.Join();
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

        private readonly Thread streamingThread;
        private readonly FirebaseRealtimeDatabaseClient client;
        private readonly CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// The database endpoint.
        /// </summary>
        public string EndPoint { get; }

        /// <summary>
        /// json serializer settings to use.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; set; }

        /// <summary>
        /// Event raised when <see cref="Value"/> is changed or updated.
        /// </summary>
        public event Action<T> OnValueChanged;

        private string json;

        private T value;

        /// <summary>
        /// The current value of the <see cref="EndPoint"/>.
        /// </summary>
        public T Value
        {
            get => value;
            set => UpdateDataSnapshot(value);
        }

        public static implicit operator T(DatabaseEndpoint<T> endpoint)
        {
            if (endpoint is null)
            {
                return default;
            }

            return endpoint.Value;
        }

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
        {
            if (other is null)
            {
                return false;
            }

            return Equals(other.Value) && EndPoint == other.EndPoint;
        }

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
        {
            if (left is null)
            {
                return false;
            }

            return left.Equals(right);
        }

        public static bool operator !=(DatabaseEndpoint<T> left, T right)
            => !(left == right);

        public static bool operator ==(T left, DatabaseEndpoint<T> right)
        {
            if (right is null)
            {
                return false;
            }

            return right.Equals(left);
        }

        public static bool operator !=(T left, DatabaseEndpoint<T> right)
            => !(left == right);

        public static bool operator ==(DatabaseEndpoint<T> left, DatabaseEndpoint<T> right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

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
            string snapshot;

            try
            {
                snapshot = await client.GetDataSnapshotAsync(EndPoint, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // Nothing
                return default;
            }
            catch (Exception e)
            {
                throw new FirebaseRealtimeDatabaseException($"Failed to get snapshot for {EndPoint}", e);
            }

            json = snapshot;
            ParseAndSetValue(json);

            return Value;
        }

        /// <summary>
        /// Manually set the <see cref="EndPoint"/> to the <see cref="newValue"/> provided.
        /// </summary>
        /// <param name="newValue">The <see cref="newValue"/> to set at the <see cref="EndPoint"/>.</param>
        /// <remarks>This will completely overwrite any existing data at the endpoint.</remarks>
        public async Task SetDataSnapshotAsync(T newValue)
        {
            var newValueJson = JsonConvert.SerializeObject(newValue, SerializerSettings);
            json = newValueJson;
            value = newValue;
            await SetDataSnapshotAsync(newValueJson);
        }

        private async Task SetDataSnapshotAsync(string newValue)
        {
            try
            {
                await client.SetDataSnapshotAsync(EndPoint, newValue, cancellationTokenSource.Token);
                OnValueChanged?.Invoke(value);
            }
            catch (TaskCanceledException)
            {
                // Nothing
            }
            catch (Exception e)
            {
                throw new FirebaseRealtimeDatabaseException($"Failed to set snapshot for {EndPoint}\n{newValue}", e);
            }
        }

        private async void UpdateDataSnapshot(T newValue)
            => await UpdateDataSnapshotAsync(newValue);

        /// <summary>
        /// Manually update or set the <see cref="EndPoint"/> to the <see cref="newValue"/> provided.
        /// </summary>
        /// <param name="newValue">The <see cref="newValue"/> to set at the <see cref="EndPoint"/>.</param>
        public async Task UpdateDataSnapshotAsync(T newValue)
        {
            var newValueJson = JsonConvert.SerializeObject(newValue, SerializerSettings);
            json = newValueJson;
            value = newValue;
            await UpdateDataSnapshotAsync(newValueJson);
        }

        private async Task UpdateDataSnapshotAsync(string newValue)
        {
            try
            {
                await client.UpdateDataSnapshotAsync(EndPoint, newValue, cancellationTokenSource.Token);
                OnValueChanged?.Invoke(value);
            }
            catch (TaskCanceledException)
            {
                // Nothing
            }
            catch (Exception e)
            {
                throw new FirebaseRealtimeDatabaseException($"Failed to update snapshot for {EndPoint}\n{newValue}", e);
            }
        }

        /// <summary>
        /// Deletes all data at the <see cref="EndPoint"/>.
        /// </summary>
        public async Task DeleteSnapshotAsync()
        {
            json = null;
            value = default;

            try
            {
                await client.DeleteDataSnapshotAsync(json, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                // Nothing
            }
            catch (Exception e)
            {
                throw new FirebaseRealtimeDatabaseException($"Failed to delete snapshot for {EndPoint}", e);
            }
        }

        private async void StartStreamingEndpoint(CancellationToken cancellationToken)
        {
            try
            {
                await client.StreamDataSnapshotChangesAsync(EndPoint, OnDatabaseEndpointValueChanged, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Nothing
            }
            catch (Exception e)
            {
                throw new FirebaseRealtimeDatabaseException($"Failed to start streaming for {EndPoint}", e);
            }
        }

        private void ParseAndSetValue(string jsonValue)
        {
            value = !string.IsNullOrWhiteSpace(jsonValue)
                ? JsonConvert.DeserializeObject<T>(jsonValue, SerializerSettings)
                : default;
            OnValueChanged?.Invoke(value);
        }

        private void OnDatabaseEndpointValueChanged(FirebaseEventType eventType, StreamedSnapShotResponse snapshot)
        {
            switch (eventType)
            {
                case FirebaseEventType.None:
                    break;
                case FirebaseEventType.Put:
                case FirebaseEventType.Patch:

                    if (snapshot.Path == "/")
                    {
                        if (json != snapshot.Data)
                        {
                            json = snapshot.Data;
                            ParseAndSetValue(json);
                        }
                    }
                    else
                    {
                        Debug.Log($"[{EndPoint}:{eventType}] {snapshot.Path} -> {snapshot.Data}");
                        // TODO update partial paths.
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
        public override string ToString() => $"{EndPoint}/{json}";
    }
}
