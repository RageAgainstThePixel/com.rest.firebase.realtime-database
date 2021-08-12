// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using Firebase.Authentication.Tests;
using NUnit.Framework;
using System;
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
            public TestJson(int value)
            {
                this.value = value;
            }

            [SerializeField]
            private int value;

            public int Value => value;
        }

        [Test]
        public void Test_1_Setup()
        {
            var authClient = new FirebaseAuthenticationClient();
            UnityTestUtils.RunAsyncTestsAsSync(async () =>
            {
                var user = await authClient.CreateUserWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd", "test user");
                Assert.IsNotNull(user);
                databaseClient = new FirebaseRealtimeDatabaseClient(authClient);
                Assert.IsNotNull(databaseClient);
                endpoint = new DatabaseEndpoint<TestJson>(databaseClient, "test");
                Assert.IsNotNull(endpoint);
            });
        }

        [Test]
        public void Test_2_SetData()
        {
            UnityTestUtils.RunAsyncTestsAsSync(async () =>
            {
                Assert.IsNotNull(endpoint);
                var result = await endpoint.GetDataSnapshotAsync();
                Assert.IsNull(result);
                Assert.IsNull(endpoint.Value);
                endpoint.Value = new TestJson(42);
            });
        }

        [Test]
        public void Test_3_GetData()
        {
            UnityTestUtils.RunAsyncTestsAsSync(async () =>
            {
                Assert.IsNotNull(endpoint);
                var endpointResult = await endpoint.GetDataSnapshotAsync();

                TestJson testJson = endpoint;

                Assert.IsTrue(endpoint == endpointResult);
                Assert.IsTrue(testJson?.Value == 42);
            });
        }

        [Test]
        public void Test_4_DeleteData()
        {
            UnityTestUtils.RunAsyncTestsAsSync(async () =>
            {
                Assert.IsNotNull(endpoint);
                await endpoint.DeleteSnapshotAsync();
                var result = await databaseClient.GetDataSnapshotAsync("test");
                Assert.IsNull(result);
                await endpoint.GetDataSnapshotAsync();
                Assert.IsNull(endpoint.Value);
            });
        }

        [Test]
        public void Test_6_TearDown()
        {
            var authClient = new FirebaseAuthenticationClient();
            UnityTestUtils.RunAsyncTestsAsSync(async () =>
            {
                await Task.Delay(1000);
                var user = await authClient.SignInWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd");
                await user.DeleteAsync();
                endpoint.Dispose();
                endpoint = null;
                databaseClient = null;
            });
        }
    }
}
