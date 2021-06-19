// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Firebase.RealtimeStorage
{
    [Serializable]
    public class StreamedSnapShotResponse
    {
        [SerializeField]
        private string path;

        public string Path => path;

        [SerializeField]
        private string data;

        public string Data => data;
    }
}
