using System;
using System.Configuration;
using Akka.Actor;
using Akka.Cluster.Utility;
using Domain.Interfaced;

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

            context.ClusterActorDiscovery = system.ActorOf(Props.Create(
                () => new ClusterActorDiscovery(null)));

            context.UserTable = system.ActorOf(Props.Create(
                () => new DistributedActorTable<long>(
                          "User", context.ClusterActorDiscovery, null, null)));

            context.UserTableContainer = system.ActorOf(Props.Create(
                () => new DistributedActorTableContainer<long>(
                          "User", context.ClusterActorDiscovery, null, null)));

            context.GameTable = system.ActorOf(Props.Create(
                () => new DistributedActorTable<long>(
                          "Game", context.ClusterActorDiscovery, typeof(IncrementalIntegerIdGenerator), null)));

            context.GameTableContainer = system.ActorOf(Props.Create(
                () => new DistributedActorTableContainer<long>(
                          "Game", context.ClusterActorDiscovery, typeof(GameActorFactory), new object[] { context })));

            var gamePairMaker = system.ActorOf(Props.Create(
                () => new GamePairMakerActor(context)));
            context.GamePairMaker = new GamePairMakerRef(gamePairMaker);

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
