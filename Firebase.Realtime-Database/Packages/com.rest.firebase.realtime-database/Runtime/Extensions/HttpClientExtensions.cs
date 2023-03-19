// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Firebase.RealtimeDatabase.Extensions
{
    /// <summary>
    /// Written by SSX-SL33PY
    /// Stack Overflow: https://stackoverflow.com/a/39450277/11145309
    /// </summary>
    internal static class HttpClientExtensions
    {
        private static readonly HttpMethod patch = new HttpMethod("PATCH");

        /// <summary>
        /// Send a PATCH request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <returns>
        /// Returns <see cref="Task{HttpResponseMessage}"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="client">The instantiated Http Client <see cref="HttpClient"/></param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The HTTP request content sent to the server.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> was null.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
            => PatchAsync(client, CreateUri(requestUri), content);

        /// <summary>
        /// Send a PATCH request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <returns>
        /// Returns <see cref="Task{HttpResponseMessage}"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="client">The instantiated Http Client <see cref="HttpClient"/></param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The HTTP request content sent to the server.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> was null.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent content)
            => PatchAsync(client, requestUri, content, CancellationToken.None);

        /// <summary>
        /// Send a PATCH request with a cancellation token as an asynchronous operation.
        /// </summary>
        /// <returns>
        /// Returns <see cref="Task{HttpResponseMessage}"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="client">The instantiated Http Client <see cref="HttpClient"/></param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The HTTP request content sent to the server.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> was null.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content, CancellationToken cancellationToken)
            => PatchAsync(client, CreateUri(requestUri), content, cancellationToken);

        /// <summary>
        /// Send a PATCH request with a cancellation token as an asynchronous operation.
        /// </summary>
        /// <returns>
        /// Returns <see cref="Task{HttpResponseMessage}"/>.The task object representing the asynchronous operation.
        /// </returns>
        /// <param name="client">The instantiated Http Client <see cref="HttpClient"/></param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The HTTP request content sent to the server.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="client"/> was null.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="requestUri"/> was null.</exception>
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            using var httpRequest = new HttpRequestMessage(patch, requestUri);
            httpRequest.Content = content;
            return client.SendAsync(httpRequest, cancellationToken);
        }

        private static Uri CreateUri(string uri)
            => string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
    }
}
