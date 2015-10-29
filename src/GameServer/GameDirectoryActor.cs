using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Interfaced;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class GameDirectoryActor : InterfacedActor<GameDirectoryActor>, IGameDirectory
    {
        private ClusterNodeContext _clusterContext;
        private List<GameDirectoryWorkerRef> _workers;
        private int _lastWorkIndex = -1;
        private Dictionary<long, Tuple<GameDirectoryWorkerRef, IGame>> _gameTable;

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
        }

        [MessageHandler]
        private void OnMessage(ActorDiscoveryMessage.ActorUp message)
        {
            _workers.Add(new GameDirectoryWorkerRef(message.Actor, this, null));
            Console.WriteLine("<><> GameDirectoryActor GOT Worker {0} <><>", message.Actor.Path);
        }

        [MessageHandler]
        private void OnMessage(ActorDiscoveryMessage.ActorDown message)
        {
            _workers.RemoveAll(w => w.Actor == message.Actor);
            Console.WriteLine("<><> GameDirectoryWorkerActor LOST GameDirectory {0} <><>", message.Actor.Path);
        }

        [MessageHandler]
        private void OnMessage(ShutdownMessage message)
        {
            Context.Stop(Self);
        }

        Task IGameDirectory.RegisterPairing(string userId)
        {
            throw new NotImplementedException();
        }

        Task IGameDirectory.UnregisterPairing()
        {
            throw new NotImplementedException();
        }

        async Task<IGame> IGameDirectory.GetOrCreateGame(long id)
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
                // TODO: Write down exception log
                Console.WriteLine(e);
            }

            if (game == null)
                return null;

            _gameTable.Add(id, game);
            return game.Item2;
        }

        Task IGameDirectory.RemoveGame(long id)
        {
            Tuple<GameDirectoryWorkerRef, IGame> game = null;
            if (_gameTable.TryGetValue(id, out game) == false)
                return Task.FromResult(0);

            _gameTable.Remove(id);
            game.Item1.WithNoReply().RemoveGame(id);

            return Task.FromResult(true);
        }

        Task<List<long>> IGameDirectory.GetGameList()
        {
            return Task.FromResult(_gameTable.Keys.ToList());
        }
    }
}
