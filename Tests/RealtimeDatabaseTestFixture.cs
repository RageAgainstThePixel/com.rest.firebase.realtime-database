// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Firebase.RealtimeDatabase.Tests
{
    [TestFixture]
    internal class RealtimeDatabaseTestFixture
    {
        private FirebaseRealtimeDatabaseClient databaseClient;
        private DatabaseEndpoint<TestJson> endpoint;

        [Serializable]
        private class TestJson
        {
            public TestJson(int value, List<int> values)
            {
                this.value = value;
                this.values = values;
            }

            [SerializeField]
            private int value;

            public int Value
            {
                get => value;
                set => this.value = value;
            }

            [SerializeField]
            private List<int> values;

            public List<int> Values => values;
        }

        [Test]
        public async Task Test_1_Setup()
        {
            var authClient = new FirebaseAuthenticationClient();
            var user = await authClient.CreateUserWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd", "test user");
            Assert.IsNotNull(user);
            databaseClient = new FirebaseRealtimeDatabaseClient(authClient);
            Assert.IsNotNull(databaseClient);
            endpoint = new DatabaseEndpoint<TestJson>(databaseClient, "test");
            Assert.IsNotNull(endpoint);
        }

        [Test]
        public async Task Test_2_PutData()
        {
            Assert.IsNotNull(endpoint);
            var result = await endpoint.GetSnapshotAsync();
            Assert.IsNull(result);
            Assert.IsNull(endpoint.Value);
            await endpoint.PutSnapshotAsync(new TestJson(42, new List<int> { 0, 1, 2 }));
        }

        [Test]
        public async Task Test_3_GetData()
        {
            Assert.IsNotNull(endpoint);
            var endpointResult = await endpoint.GetSnapshotAsync();

            TestJson testJson = endpoint;

            Assert.IsTrue(endpoint == endpointResult);
            Assert.IsNotNull(testJson);
            Assert.IsTrue(testJson.Value == 42);
            Assert.IsNotEmpty(testJson.Values);
            Assert.IsTrue(testJson.Values.Contains(0));
            Assert.IsTrue(testJson.Values.Contains(1));
            Assert.IsTrue(testJson.Values.Contains(2));
        }

        [Test]
        public async Task Test_4_PatchData()
        {
            Assert.IsNotNull(endpoint);
            var endpointResult = await endpoint.GetSnapshotAsync();

            TestJson testJson = endpoint;

            Assert.IsTrue(endpoint == endpointResult);
            Assert.IsNotNull(testJson);
            Assert.IsTrue(testJson.Value == 42);
            Assert.IsNotEmpty(testJson.Values);
            Assert.IsTrue(testJson.Values.Contains(0));
            Assert.IsTrue(testJson.Values.Contains(1));
            Assert.IsTrue(testJson.Values.Contains(2));
            testJson.Values.Add(9001);
            testJson.Value = 128;
            await endpoint.PatchSnapshotAsync(testJson);
            endpointResult = await endpoint.GetSnapshotAsync();
            Assert.IsTrue(endpoint == endpointResult);
            Assert.IsNotNull(testJson);
            Assert.IsTrue(testJson.Value == 128);
            Assert.IsNotEmpty(testJson.Values);
            Assert.IsTrue(testJson.Values.Contains(0));
            Assert.IsTrue(testJson.Values.Contains(1));
            Assert.IsTrue(testJson.Values.Contains(2));
            Assert.IsTrue(testJson.Values.Contains(9001));
        }

        [Test]
        public async Task Test_5_DeleteData()
        {
            Assert.IsNotNull(endpoint);
            await endpoint.DeleteSnapshotAsync();
            var result = await databaseClient.GetSnapshotAsync("test");
            Assert.IsNull(result);
            await endpoint.GetSnapshotAsync();
            Assert.IsNull(endpoint.Value);
        }

        [Test]
        public async Task Test_6_TearDown()
        {
            Assert.IsNull(endpoint.Value);
            endpoint.Dispose();
            endpoint = null;
            Assert.IsNull(endpoint);
            var authClient = new FirebaseAuthenticationClient();
            var user = await authClient.SignInWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd");
            Assert.IsNotNull(user);
            await user.DeleteAsync();
        }
    }
}
