// Licensed under the MIT License. See LICENSE in the project root for license information.

using Firebase.Authentication;
using Firebase.RealtimeStorage;
using NUnit.Framework;

namespace Firebase.RealtimeDatabase.Tests
{
    [TestFixture]
    internal class RealtimeDatabaseTestFixture
    {
        private const string TestJsonValue = "{\"value\":42}";

        [SetUp]
        public void Setup()
        {
            var authClient = new FirebaseAuthenticationClient();
            var user = authClient.CreateUserWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd", "test user").Result;
            Assert.IsNotNull(user);
        }

        [Test]
        public void Test_1_CreateInstance()
        {
            var authClient = new FirebaseAuthenticationClient();
            var databaseClient = new FirebaseRealtimeDatabaseClient(authClient);
            Assert.IsNotNull(databaseClient);
        }

        [Test]
        public void Test_2_SetData()
        {
            var authClient = new FirebaseAuthenticationClient();
            var databaseClient = new FirebaseRealtimeDatabaseClient(authClient);
            authClient.SignInWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd").Wait();

            var result = databaseClient.SetDataSnapshotAsync("test", TestJsonValue).Result;
            Assert.IsTrue(result == TestJsonValue);
        }

        [Test]
        public void Test_3_GetData()
        {
            var authClient = new FirebaseAuthenticationClient();
            var databaseClient = new FirebaseRealtimeDatabaseClient(authClient);
            authClient.SignInWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd").Wait();

            var result = databaseClient.GetDataSnapshotAsync("test").Result;
            Assert.IsTrue(result == TestJsonValue);
        }

        [Test]
        public void Test_4_DeleteData()
        {
            var authClient = new FirebaseAuthenticationClient();
            var databaseClient = new FirebaseRealtimeDatabaseClient(authClient);
            authClient.SignInWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd").Wait();

            databaseClient.DeleteDataSnapshotAsync("test").Wait();
            var result = databaseClient.GetDataSnapshotAsync("test").Result;
            Assert.IsNull(result);
        }

        [TearDown]
        public void TearDown()
        {
            var authClient = new FirebaseAuthenticationClient();
            var user = authClient.SignInWithEmailAndPasswordAsync("test@email.com", "tempP@ssw0rd").Result;
            user.DeleteAsync().Wait();
        }
    }
}
