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
        private ClusterNodeContext _clusterContext;
        private List<Tuple<string, IUserPairingObserver>> _pairingQueue;

        public GamePairMakerActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            _clusterContext.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.ActorUp { Actor = Self, Type = typeof(IGamePairMaker) },
                Self);

            _pairingQueue = new List<Tuple<string, IUserPairingObserver>>();
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

            // NOTE: It's just only for bot matching.
            // TODO: DO SAME WITH USERS

            var entry = _pairingQueue[0];
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

            entry.Item2.MakePair(gameId, "bot");
        }

        [ExtendedHandler]
        void RegisterPairing(string userId, IUserPairingObserver observer)
        {
            // NOTE: If more perfermance is required, we can optimize here by using map

            if (_pairingQueue.Any(i => i.Item1 == userId))
                throw new ResultException(ResultCodeType.AlreadyPairingRegistered);

            _pairingQueue.Add(Tuple.Create(userId, observer));
        }

        [ExtendedHandler]
        void UnregisterPairing(string userId)
        {
            _pairingQueue.RemoveAll(i => i.Item1 == userId);
        }
    }
}
