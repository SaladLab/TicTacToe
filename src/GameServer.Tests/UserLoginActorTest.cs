using Akka.Actor;
using Domain.Interfaced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Akka.Cluster.Utility;
using Xunit;
using Akka.Interfaced.SlimSocket.Server;

namespace GameServer.Tests
{
    public class UserLoginActorTest : IClassFixture<MongoDbStorageFixture>, IClassFixture<ClusterContextFixture>
    {
        private ClusterNodeContext _clusterContext;

        public UserLoginActorTest(ClusterContextFixture clusterContext)
        {
            _clusterContext = clusterContext.Context;
        }

        private IActorRef _clientSession;
        private IActorRef _userLoginActor;

        private UserLoginRef CreateUserLogin()
        {
            var system = _clusterContext.System;
            _clientSession = system.ActorOf(
                Props.Create(() => new ClientSession(null, null, null, null)));
            _userLoginActor = system.ActorOf(
                Props.Create(() => new UserLoginActor(_clusterContext, _clientSession, new IPEndPoint(0, 0))));
            return new UserLoginRef(_userLoginActor);
        }

        [Fact]
        public async Task Test_UserLogin_NewUser_Succeed()
        {
            var userLogin = CreateUserLogin();
            var ret = await userLogin.Login("test", "1234", 0);
            Assert.Equal("TEST", ret.UserContext.Data.Name);
        }

        [Fact]
        public async Task Test_UserLogin_ExistingUser_Succeed()
        {
            var userLogin = CreateUserLogin();
            var ret = await userLogin.Login("test", "1234", 0);
            Assert.Equal("TEST", ret.UserContext.Data.Name);

            _clientSession.Tell(PoisonPill.Instance);

            var userLogin2 = CreateUserLogin();
            var ret2 = await userLogin.Login("test", "1234", 0);
            Assert.Equal("TEST", ret2.UserContext.Data.Name);
        }
    }
}
