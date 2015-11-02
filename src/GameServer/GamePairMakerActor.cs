using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Interfaced;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    public class GamePairMakerActor : InterfacedActor<GamePairMakerActor>, IExtendedInterface<IGamePairMaker>
    {
        private ILog _logger = LogManager.GetLogger("GamePairMaker");
        private readonly ClusterNodeContext _clusterContext;
        private static readonly TimeSpan BotPairingTimeout = TimeSpan.FromSeconds(3);

        private class QueueEntity
        {
            public string UserId;
            public IUserPairingObserver Observer;
            public DateTime EnqueueTime;
        }

        // NOTE: If more performance required, lookup could be optimized further.
        private readonly List<QueueEntity> _pairingQueue;

        public GamePairMakerActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            _clusterContext.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.ActorUp { Actor = Self, Type = typeof(IGamePairMaker) },
                Self);

            _pairingQueue = new List<QueueEntity>();
        }

        protected override Task OnPreStart()
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), Self, new Schedule(), null);
            return Task.FromResult(0);
        }

        [MessageHandler]
        private void OnShutdown(ShutdownMessage message)
        {
            Context.Stop(Self);
        }

        private class Schedule
        {
        };

        [MessageHandler]
        private async Task OnSchedule(Schedule tick)
        {
            if (_pairingQueue.Any() == false || _clusterContext.GameDirectory == null)
                return;

            // Pairing for two users

            while (_pairingQueue.Count >= 2)
            {
                var entry0 = _pairingQueue[0];
                var entry1 = _pairingQueue[1];

                _pairingQueue.RemoveAt(0);
                _pairingQueue.RemoveAt(0);

                var gameId = 0L;
                try
                {
                    var ret = await _clusterContext.GameDirectory.CreateGame(new CreateGameParam { WithBot = false });
                    gameId = ret.Item1;
                }
                catch (Exception e)
                {
                    _logger.ErrorFormat("Failed to create game", e);
                    return;
                }

                entry0.Observer.MakePair(gameId, entry1.UserId);
                entry1.Observer.MakePair(gameId, entry0.UserId);
            }

            // Pairing an user with a bot

            if (_pairingQueue.Count == 1)
            {
                var entry = _pairingQueue[0];
                if ((DateTime.UtcNow - entry.EnqueueTime) > BotPairingTimeout)
                {
                    _pairingQueue.RemoveAt(0);

                    var gameId = 0L;
                    try
                    {
                        var ret = await _clusterContext.GameDirectory.CreateGame(new CreateGameParam { WithBot = true });
                        gameId = ret.Item1;
                    }
                    catch (Exception e)
                    {
                        _logger.ErrorFormat("Failed to create game", e);
                        return;
                    }

                    entry.Observer.MakePair(gameId, "bot");
                }
            }
        }

        [ExtendedHandler]
        void RegisterPairing(string userId, IUserPairingObserver observer)
        {
            if (_pairingQueue.Any(i => i.UserId == userId))
                throw new ResultException(ResultCodeType.AlreadyPairingRegistered);

            _pairingQueue.Add(new QueueEntity
            {
                UserId = userId,
                Observer = observer,
                EnqueueTime = DateTime.UtcNow
            });
        }

        [ExtendedHandler]
        void UnregisterPairing(string userId)
        {
            _pairingQueue.RemoveAll(i => i.UserId == userId);
        }
    }
}
