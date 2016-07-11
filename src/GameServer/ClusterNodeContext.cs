using Aim.ClusterNode;
using Akka.Cluster.Utility;
using Domain;

namespace GameServer
{
    public class ClusterNodeContext : ClusterNodeContextBase
    {
        [ClusterActor("User")]
        public DistributedActorTableRef<long> UserTable;

        [ClusterActor("Game")]
        public DistributedActorTableRef<long> GameTable;

        [ClusterActor(nameof(IGamePairMaker))]
        public GamePairMakerRef GamePairMaker;
    }
}
