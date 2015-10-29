using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;
using System.Collections.Generic;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    public class GameDirectoryWorkerActor : InterfacedActor<GameDirectoryWorkerActor>, IGameDirectoryWorker
    {
        private ILog _logger = LogManager.GetLogger("GameDirectoryWorker");
        private readonly ClusterNodeContext _context;
        private GameDirectoryRef _gameDirectory;
        private readonly Dictionary<long, IGame> _gameTable;
        private int _gameActorCount;
        private bool _isStopped;

        public GameDirectoryWorkerActor(ClusterNodeContext context)
        {
            _context = context;

            _context.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.ActorUp { Actor = Self, Type = typeof(IGameDirectoryWorker) },
                Self);
            _context.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.WatchActor { Type = typeof(IGameDirectory) },
                Self);

            _gameTable = new Dictionary<long, IGame>();
        }

        [MessageHandler]
        private void OnMessage(ActorDiscoveryMessage.ActorUp message)
        {
            _gameDirectory = new GameDirectoryRef(message.Actor, this, null);
            Console.WriteLine("<><> GameDirectoryWorkerActor GOT GameDirectory {0} <><>", message.Actor.Path);

        }

        [MessageHandler]
        private void OnMessage(ActorDiscoveryMessage.ActorDown message)
        {
            if (_gameDirectory != null && _gameDirectory.Actor.Equals(message.Actor))
            {
                _gameDirectory = null;
                Console.WriteLine("<><> GameDirectoryWorkerActor LOST GameDirectory {0} <><>", message.Actor.Path);
            }
        }

        [MessageHandler]
        private void OnMessage(ShutdownMessage message)
        {
            if (_isStopped)
                return;

            _logger.Info("Stop");
            _isStopped = true;

            // stop all running client sessions

            if (_gameActorCount > 0)
            {
                Context.ActorSelection("*").Tell(InterfacedPoisonPill.Instance);
            }
            else
            {
                Context.Stop(Self);
            }
        }

        [MessageHandler]
        private void OnMessage(Terminated message)
        {
            _gameActorCount -= 1;
            if (_isStopped && _gameActorCount == 0)
                Context.Stop(Self);
        }

        Task<IGame> IGameDirectoryWorker.CreateGame(long id)
        {
            // create game actor

            IActorRef gameActor = null;
            try
            {
                gameActor = Context.ActorOf(Props.Create<GameActor>(_context, id));
                Context.Watch(gameActor);
                _gameActorCount += 1;
            }
            catch (Exception)
            {
                return Task.FromResult((IGame)null);
            }

            // register it at local directory and return

            var game = new GameRef(gameActor);
            _gameTable.Add(id, game);
            return Task.FromResult((IGame)game);
        }

        Task IGameDirectoryWorker.RemoveGame(long id)
        {
            IGame game;
            if (_gameTable.TryGetValue(id, out game) == false)
                return Task.FromResult(0);

            ((GameRef)game).Actor.Tell(InterfacedPoisonPill.Instance);
            _gameTable.Remove(id);
            return Task.FromResult(0);
        }
    }
}
