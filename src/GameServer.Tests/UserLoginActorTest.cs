using Akka.Actor;
using Domain.Interfaced;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Akka.Cluster.Utility;
using Xunit;

namespace GameServer.Tests
{
    public class UserLoginActorTest : IClassFixture<MongoDbStorageFixture>
    {
        private ClusterNodeContext PrepareTest()
        {
            // force interface assembly to be loaded before creating ProtobufSerializer

            var type = typeof(IUser);
            if (type == null)
                throw new InvalidProgramException("!");

            // create system

            var system = ActorSystem.Create("Test");

            // context 

            var context = new ClusterNodeContext { System = system };

            var gameDirectory = system.ActorOf(Props.Create<GameDirectoryActor>(context));
            var gamePairMaker = system.ActorOf(Props.Create<GamePairMakerActor>(context));
            var userDirectory = system.ActorOf(Props.Create<UserDirectoryActor>(context));

            context.ClusterActorDiscovery = system.ActorOf(Props.Create(() => new ClusterActorDiscovery(null)));
            context.GameDirectory = new GameDirectoryRef(gameDirectory);
            context.GamePairMaker = new GamePairMakerRef(gamePairMaker);
            context.UserDirectory = new UserDirectoryRef(userDirectory);

            return context;
        }

        [Fact]
        public async Task Test_Work()
        {
            var context = PrepareTest();
            var clientSession = context.System.ActorOf(Props.Create(() => new ClientSession(context, null)));
            var userLoginActor = context.System.ActorOf(Props.Create(() => new UserLoginActor(context, clientSession, new IPEndPoint(0, 0))));
            var userLogin = new UserLoginRef(userLoginActor);
            var ret = await userLogin.Login("test", "1234", 0);
            Assert.Equal("TEST", ret.UserContext.Data.Name);
            // TODO: CHECK UserContext DATA
            // TODO: Check UserDirectoryActor
        }
    }
}
