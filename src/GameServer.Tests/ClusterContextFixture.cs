using System;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Cluster.Utility;
using Domain;

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
        }

        public void Initialize(ActorSystem system)
        {
            var context = new ClusterNodeContext { System = system };

            context.ClusterActorDiscovery = system.ActorOf(
                Props.Create(() => new ClusterActorDiscovery(null)));

            context.UserTable = new DistributedActorTableRef<long>(system.ActorOf(
                Props.Create(() => new DistributedActorTable<long>(
                    "User", context.ClusterActorDiscovery, null, null)),
                "UserTable"));

            var userTableContainer = system.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>(
                    "User", context.ClusterActorDiscovery, typeof(UserActorFactory), new object[] { context }, InterfacedPoisonPill.Instance)),
                "UserTableContainer");

            context.GameTable = new DistributedActorTableRef<long>(system.ActorOf(
                Props.Create(() => new DistributedActorTable<long>(
                    "Game", context.ClusterActorDiscovery, typeof(IncrementalIntegerIdGenerator), null)),
                "GameTable"));

            var gameTableContainer = system.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>(
                    "Game", context.ClusterActorDiscovery, typeof(GameActorFactory), new object[] { context }, InterfacedPoisonPill.Instance)),
                "GameTableContainer");

            var gamePairMaker = system.ActorOf(Props.Create(() => new GamePairMakerActor(context)));
            context.GamePairMaker = gamePairMaker.Cast<GamePairMakerRef>();

            Context = context;
        }

        public void Dispose()
        {
            if (Context == null)
                return;

            Context.System.Terminate();
            Context = null;
        }
    }
}
