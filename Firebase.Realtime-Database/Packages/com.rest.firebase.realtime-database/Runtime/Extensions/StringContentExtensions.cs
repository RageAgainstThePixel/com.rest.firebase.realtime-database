// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http;
using System.Text;

namespace Firebase.RealtimeDatabase.Extensions
{
    internal static class StringContentExtensions
    {
        private const string JsonMediaType = "application/json";

        public static StringContent ToJsonStringContent(this string input)
            => new StringContent(input, Encoding.UTF8, JsonMediaType);
    }
}
