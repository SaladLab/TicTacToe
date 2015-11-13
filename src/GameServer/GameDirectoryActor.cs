using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class GameDirectoryActor : InterfacedActor<GameDirectoryActor>, IExtendedInterface<IGameDirectory>
    {
        private ILog _logger = LogManager.GetLogger("GameDirectory");
        private ClusterNodeContext _clusterContext;
        private List<GameDirectoryWorkerRef> _workers;
        private int _lastWorkIndex = -1;
        private long _lastGameId;
        private Dictionary<long, Tuple<GameDirectoryWorkerRef, IGame>> _gameTable;

        public GameDirectoryActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.RegisterActor(Self, nameof(IGameDirectory)),
                Self);
            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.MonitorActor(nameof(IGameDirectoryWorker)),
                Self);

            _workers = new List<GameDirectoryWorkerRef>();
            _gameTable = new Dictionary<long, Tuple<GameDirectoryWorkerRef, IGame>>();
        }

        [MessageHandler]
        private void OnMessage(ClusterActorDiscoveryMessage.ActorUp message)
        {
            _workers.Add(new GameDirectoryWorkerRef(message.Actor, this, null));
            _logger.InfoFormat("Registered Actor({0})", message.Actor.Path);
        }

        [MessageHandler]
        private void OnMessage(ClusterActorDiscoveryMessage.ActorDown message)
        {
            _workers.RemoveAll(w => w.Actor.Equals(message.Actor));
            _logger.InfoFormat("Unregistered Actor({0})", message.Actor.Path);
        }

        [MessageHandler]
        private void OnMessage(ShutdownMessage message)
        {
            Context.Stop(Self);
        }

        private long IssueNewGameId()
        {
            _lastGameId += 1;
            return _lastGameId;
        }

        [ExtendedHandler]
        private IGame GetGame(long id)
        {
            Tuple<GameDirectoryWorkerRef, IGame> game;
            return _gameTable.TryGetValue(id, out game) ? game.Item2 : null;
        }

        [ExtendedHandler]
        private async Task<Tuple<long, IGame>> CreateGame(CreateGameParam param)
        {
            // pick a worker for creating GameActor by round-robin fashion.

            _lastWorkIndex = (_lastWorkIndex + 1) % _workers.Count;
            var worker = _workers[_lastWorkIndex];

            var gameId = IssueNewGameId();
            Tuple<GameDirectoryWorkerRef, IGame> gameValue;

            try
            {
                gameValue = Tuple.Create(worker, await worker.CreateGame(gameId, param));
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Worker({0} is failed to create game({1})",
                                    e, worker.Actor.Path, gameId);
                return null;
            }

            _gameTable.Add(gameId, gameValue);
            return Tuple.Create(gameId, gameValue.Item2);
        }

        [ExtendedHandler]
        private void RemoveGame(long id)
        {
            Tuple<GameDirectoryWorkerRef, IGame> game = null;
            if (_gameTable.TryGetValue(id, out game) == false)
                return;

            _gameTable.Remove(id);
            game.Item1.WithNoReply().RemoveGame(id);
        }

        [ExtendedHandler]
        List<long> GetGameList()
        {
            return _gameTable.Keys.ToList();
        }
    }
}
