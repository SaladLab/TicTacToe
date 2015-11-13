using Akka.Actor;
using Domain.Interfaced;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;

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
            var logger = LogManager.GetLogger("Test");
            var system = _clusterContext.System;

            _clientSession = system.ActorOf(Props.Create(
                () => new ClientSession(logger, null, new TcpConnectionSettings(), CreateInitialActor)));

            return new UserLoginRef(_userLoginActor);
        }

        private Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context, Socket socket)
        {
            return new[]
            {
                    Tuple.Create(
                        context.ActorOf(Props.Create(
                            () => new UserLoginActor(_clusterContext, context.Self, new IPEndPoint(0, 0)))),
                        typeof(IUserLogin))
                };
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
