using Akka.Cluster.Utility;
using Akka.Interfaced;
using Domain;

namespace GameServer
{
    public class ClusterNodeContextUpdater : InterfacedActor
    {
        private readonly ClusterNodeContext _clusterContext;

        public ClusterNodeContextUpdater(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;
        }

        protected override void PreStart()
        {
            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.MonitorActor("User"), Self);

            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.MonitorActor("Game"), Self);

            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.MonitorActor(nameof(IGamePairMaker)), Self);
        }

        [MessageHandler]
        private void OnMessage(ClusterActorDiscoveryMessage.ActorUp m)
        {
            switch (m.Tag)
            {
                case "User":
                    _clusterContext.UserTable = m.Actor;
                    break;

                case "Game":
                    _clusterContext.GameTable = m.Actor;
                    break;

                case nameof(IGamePairMaker):
                    _clusterContext.GamePairMaker = m.Actor.Cast<GamePairMakerRef>();
                    break;
            }
        }

        [MessageHandler]
        private void OnMessage(ClusterActorDiscoveryMessage.ActorDown m)
        {
            switch (m.Tag)
            {
                case "User":
                    _clusterContext.UserTable = null;
                    break;

                case "Game":
                    _clusterContext.GameTable = null;
                    break;

                case nameof(IGamePairMaker):
                    _clusterContext.GamePairMaker = null;
                    break;
            }
        }
    }
}
