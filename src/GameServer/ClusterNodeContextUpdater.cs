using System;
using System.Collections.Generic;
using Akka;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Domain.Interfaced;

namespace GameServer
{
    public class ClusterNodeContextUpdater : InterfacedActor<ClusterNodeContextUpdater>
    {
        private readonly ClusterNodeContext _clusterContext;

        public ClusterNodeContextUpdater(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;
        }

        protected override void PreStart()
        {
            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.MonitorActor(nameof(IUserDirectory)), Self);

            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.MonitorActor(nameof(IGameDirectory)), Self);

            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.MonitorActor(nameof(IGamePairMaker)), Self);
        }

        [MessageHandler]
        private void OnMessage(ClusterActorDiscoveryMessage.ActorUp m)
        {
            switch (m.Tag)
            {
                case nameof(IUserDirectory):
                    _clusterContext.UserDirectory = new UserDirectoryRef(m.Actor);
                    break;

                case nameof(IGameDirectory):
                    _clusterContext.GameDirectory = new GameDirectoryRef(m.Actor);
                    break;

                case nameof(IGamePairMaker):
                    _clusterContext.GamePairMaker = new GamePairMakerRef(m.Actor);
                    break;
            }
        }

        [MessageHandler]
        private void OnMessage(ClusterActorDiscoveryMessage.ActorDown m)
        {
            switch (m.Tag)
            {
                case nameof(IUserDirectory):
                    _clusterContext.UserDirectory = null;
                    break;

                case nameof(IGameDirectory):
                    _clusterContext.GameDirectory = null;
                    break;

                case nameof(IGamePairMaker):
                    _clusterContext.GamePairMaker = null;
                    break;
            }
        }
    }
}
