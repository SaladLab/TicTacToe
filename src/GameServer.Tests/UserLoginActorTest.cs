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
            await _client.PrepareUserAsync("test", "1234", "CreatedUser");
            Assert.Equal("CreatedUser", _client.UserContext.Data.Name);
        }

        [Fact]
        public async Task Test_UserLogin_ExistingUser_Succeed()
        {
            await _client.PrepareUserAsync("test", "1234", "CreatedUser");

            _client.ChannelRef.WithNoReply().Close();
            await Task.Delay(100);

            _client = new MockClient(_clusterContext);
            await _client.PrepareUserAsync("test", "1234");
            Assert.Equal("CreatedUser", _client.UserContext.Data.Name);
        }
    }
}
