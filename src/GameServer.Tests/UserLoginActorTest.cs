using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced.TestKit;
using Akka.TestKit;
using Akka.TestKit.Xunit2;
using Domain.Interfaced;
using Xunit;

namespace GameServer.Tests
{
    public class UserLoginActorTest : TestKit, IClassFixture<MongoDbStorageFixture>, IClassFixture<ClusterContextFixture>
    {
        private ClusterNodeContext _clusterContext;
        private TestActorRef<TestActorBoundSession> _clientSession;

        public UserLoginActorTest(ClusterContextFixture clusterContext)
        {
            clusterContext.Initialize(Sys);
            _clusterContext = clusterContext.Context;
        }

        private UserLoginRef CreateUserLogin()
        {
            var system = _clusterContext.System;

            _clientSession = new TestActorRef<TestActorBoundSession>(
                system, Props.Create(() => new TestActorBoundSession(CreateInitialActor)));

            return new UserLoginRef(null, _clientSession.UnderlyingActor.GetRequestWaiter(1), null);
        }

        private Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context)
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
            var ret2 = await userLogin2.Login("test", "1234", 0);
            Assert.Equal("TEST", ret2.UserContext.Data.Name);
        }
    }
}
