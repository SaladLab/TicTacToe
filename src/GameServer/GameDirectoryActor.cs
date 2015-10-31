using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Interfaced;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    public class GameDirectoryActor : InterfacedActor<GameDirectoryActor>, IExtendedInterface<IGameDirectory>
    {
        private ILog _logger = LogManager.GetLogger("GameDirectory");
        private ClusterNodeContext _clusterContext;
        private List<GameDirectoryWorkerRef> _workers;
        private int _lastWorkIndex = -1;
        private long _lastGameId;
        private Dictionary<long, Tuple<GameDirectoryWorkerRef, IGame>> _gameTable;
        private List<Tuple<string, IUserPairingObserver>> _pairingQueue;

        public GameDirectoryActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            _clusterContext.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.ActorUp { Actor = Self, Type = typeof(IGameDirectory) },
                Self);
            _clusterContext.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.WatchActor { Type = typeof(IGameDirectoryWorker) },
                Self);

            _workers = new List<GameDirectoryWorkerRef>();
            _gameTable = new Dictionary<long, Tuple<GameDirectoryWorkerRef, IGame>>();
            _pairingQueue = new List<Tuple<string, IUserPairingObserver>>();
        }

        [MessageHandler]
        private void OnMessage(ActorDiscoveryMessage.ActorUp message)
        {
            _workers.Add(new GameDirectoryWorkerRef(message.Actor, this, null));
            _logger.InfoFormat("Registered Actor({0})", message.Actor.Path);
        }

        [MessageHandler]
        private void OnMessage(ActorDiscoveryMessage.ActorDown message)
        {
            _workers.RemoveAll(w => w.Actor == message.Actor);
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
        private async Task<IGame> GetOrCreateGame(long id)
        {
            Tuple<GameDirectoryWorkerRef, IGame> game = null;
            if (_gameTable.TryGetValue(id, out game))
                return game.Item2;

            if (_workers.Count == 0)
                return null;

            // pick a worker for creating GameActor by round-robin fashion.

            _lastWorkIndex = (_lastWorkIndex + 1) % _workers.Count;
            var worker = _workers[_lastWorkIndex];

            try
            {
                game = Tuple.Create(worker, await worker.CreateGame(id));
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Worker({0} is failed to create game({1})",
                                    e, worker.Actor.Path, id);
            }

            if (game == null)
                return null;

            _gameTable.Add(id, game);
            return game.Item2;
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

        [ExtendedHandler]
        void RegisterPairing(string userId, IUserPairingObserver observer)
        {
            // TEST
            observer.MakePair(IssueNewGameId(), "bot");
            return;

            // NOTE: If more perfermance, we can optimize here by using map

            if (_pairingQueue.Any(i => i.Item1 == userId))
                throw new ResultException(ResultCodeType.AlreadyPairingRegistered);

            // If there is an opponent, both are paired to match.
            // Otherwise enqueue an user to waiting list.

            if (_pairingQueue.Any())
            {
                var opponent = _pairingQueue[0];
                _pairingQueue.RemoveAt(0);

                var gameId = IssueNewGameId();
                observer.MakePair(gameId, opponent.Item1);
                opponent.Item2.MakePair(gameId, userId);
            }
            else
            {
                _pairingQueue.Add(Tuple.Create(userId, observer));
            }
        }

        [ExtendedHandler]
        void UnregisterPairing(string userId)
        {
            _pairingQueue.RemoveAll(i => i.Item1 == userId);
        }
    }
}
