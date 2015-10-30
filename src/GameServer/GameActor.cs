using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class GameActor : InterfacedActor<GameActor>, IExtendedInterface<IGame>
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private long _id;

        public GameActor(ClusterNodeContext clusterContext, long id)
        {
            _logger = LogManager.GetLogger($"GameActor({id})");
            _clusterContext = clusterContext;
            _id = id;
        }

        [ExtendedHandler]
        GameInfo Join(string userId, IGameObserver observer)
        {
            return new GameInfo();
        }

        [ExtendedHandler]
        void Leave(string userId)
        {
        }
    }
}
