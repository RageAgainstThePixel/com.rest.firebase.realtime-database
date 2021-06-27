// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Firebase.RealtimeDatabase
{
    [Serializable]
    public class StreamedSnapShotResponse
    {
        [SerializeField]
        private string path;

        public string Path => path;

        public string Data { get; internal set; }
    }
}
