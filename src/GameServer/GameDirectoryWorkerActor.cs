using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using Akka.Actor;
using System.Collections.Generic;
using Akka.Cluster.Utility;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class GameDirectoryWorkerActor : InterfacedActor<GameDirectoryWorkerActor>, IGameDirectoryWorker
    {
        private ILog _logger = LogManager.GetLogger("GameDirectoryWorker");
        private readonly ClusterNodeContext _clusterContext;
        private readonly Dictionary<long, IGame> _gameTable;
        private int _gameActorCount;
        private bool _isStopped;

        public GameDirectoryWorkerActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessage.RegisterActor(Self, nameof(IGameDirectoryWorker)),
                Self);

            _gameTable = new Dictionary<long, IGame>();
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
                // TODO: specific target only
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

        Task<IGame> IGameDirectoryWorker.CreateGame(long id, CreateGameParam param)
        {
            // create game actor

            IActorRef gameActor;
            try
            {
                gameActor = Context.ActorOf(Props.Create(() => new GameActor(_clusterContext, id, param)));
                Context.Watch(gameActor);
                _gameActorCount += 1;
            }
            catch (Exception)
            {
                return Task.FromResult((IGame)null);
            }

            // create bot if required

            if (param.WithBot)
            {
                Context.ActorOf(Props.Create<GameBotActor>(_clusterContext, new GameRef(gameActor), 0, "bot"));
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
