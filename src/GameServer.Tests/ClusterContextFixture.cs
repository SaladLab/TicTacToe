using System;
using System.Configuration;
using Akka.Actor;
using Akka.Cluster.Utility;
using Domain.Interfaced;
using MongoDB.Driver;

namespace GameServer.Tests
{
    public class ClusterContextFixture : IDisposable
    {
        public ClusterNodeContext Context { get; private set; }

        public ClusterContextFixture()
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

            Context = context;
        }

        public void Dispose()
        {
            if (Context == null)
                return;

            Context.System.Shutdown();
            Context = null;
        }
    }
}
