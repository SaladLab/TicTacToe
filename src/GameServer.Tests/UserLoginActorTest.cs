using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced.TestKit;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Domain;
using Xunit;

namespace GameServer.Tests
{
    public class UserLoginActorTest : TestKit, IClassFixture<MongoDbStorageFixture>, IClassFixture<ClusterContextFixture>
    {
        private ClusterNodeContext _clusterContext;
        private MockClient _client;

        public UserLoginActorTest(ClusterContextFixture clusterContext)
        {
            clusterContext.Initialize(Sys);
            _clusterContext = clusterContext.Context;
            _client = new MockClient(_clusterContext);
        }

        [Fact]
        public async Task Test_UserLogin_NewUser_Succeed()
        {
            var ret = await _client.LoginAsync("test", "1234");
            Assert.Equal("TEST", ret.UserContext.Data.Name);
        }

        [Fact]
        public async Task Test_UserLogin_ExistingUser_Succeed()
        {
            var ret = await _client.LoginAsync("test", "1234");

            _client.ClientSessionActor.Tell(PoisonPill.Instance);

            _client = new MockClient(_clusterContext);
            var ret2 = await _client.LoginAsync("test", "1234");
            Assert.Equal("TEST", ret2.UserContext.Data.Name);
        }
    }
}
