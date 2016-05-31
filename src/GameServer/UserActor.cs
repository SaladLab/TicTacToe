using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain;

namespace GameServer
{
    [Log]
    [ResponsiveException(typeof(ResultException))]
    public class UserActor : InterfacedActor, IUser, IGameUserObserver
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private IActorRef _clientSession;
        private long _id;
        private TrackableUserContext _userContext;
        private IUserEventObserver _userEventObserver;
        private Dictionary<long, GameRef> _joinedGameMap;

        public UserActor(ClusterNodeContext clusterContext, IActorRef clientSession,
                         long id, TrackableUserContext userContext, IUserEventObserver observer)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clusterContext = clusterContext;
            _clientSession = clientSession;
            _id = id;
            _userContext = userContext;
            _userEventObserver = observer;
            _joinedGameMap = new Dictionary<long, GameRef>();
        }

        private void UnlinkAll()
        {
            foreach (var game in _joinedGameMap.Values)
                game.WithNoReply().Leave(_id);
            _joinedGameMap.Clear();
        }

        [MessageHandler]
        protected void OnMessage(ActorBoundSessionMessage.SessionTerminated message)
        {
            UnlinkAll();
            Context.Stop(Self);
        }

        Task IUser.RegisterPairing(IUserPairingObserver observer)
        {
            return _clusterContext.GamePairMaker.RegisterPairing(_id, _userContext.Data.Name, observer);
        }

        Task IUser.UnregisterPairing()
        {
            return _clusterContext.GamePairMaker.UnregisterPairing(_id);
        }

        async Task<Tuple<IGamePlayer, int, GameInfo>> IUser.JoinGame(long gameId, IGameObserver observer)
        {
            if (_joinedGameMap.ContainsKey(gameId))
                throw new ResultException(ResultCodeType.NeedToBeOutOfGame);

            // Try to get game ref

            var reply = await _clusterContext.GameTable.Ask<DistributedActorTableMessage<long>.GetOrCreateReply>(
                new DistributedActorTableMessage<long>.GetOrCreate(gameId, null));
            if (reply.Actor == null)
                throw new ResultException(ResultCodeType.GameNotFound);

            var game = new GameRef(reply.Actor, this, null);

            // Let's enter the game !

            var observerForMe = CreateObserver<IGameUserObserver>();
            var joinRet = await game.Join(_id, _userContext.Data.Name, observer, observerForMe);

            // Bind an player actor with client session

            var reply2 = await _clientSession.Ask<ActorBoundSessionMessage.BindReply>(
                new ActorBoundSessionMessage.Bind(game.Actor, typeof(IGamePlayer), _id));
            if (reply2.ActorId == 0)
            {
                await game.Leave(_id);
                _logger.Error($"Failed in binding GamePlayer");
                throw new ResultException(ResultCodeType.InternalError);
            }

            _joinedGameMap[gameId] = game;
            return Tuple.Create((IGamePlayer)BoundActorRef.Create<GamePlayerRef>(reply2.ActorId), joinRet.Item1, joinRet.Item2);
        }

        async Task IUser.LeaveGame(long gameId)
        {
            GameRef game;
            if (_joinedGameMap.TryGetValue(gameId, out game) == false)
                throw new ResultException(ResultCodeType.NeedToBeInGame);

            // Let's exit from the game !

            await game.Leave(_id);

            // Unbind an player actor with client session

            _clientSession.Tell(new ActorBoundSessionMessage.Unbind(game.Actor));
            _joinedGameMap.Remove(gameId);
        }

        void IGameUserObserver.Begin(long gameId)
        {
            _logger.TraceFormat("IGameUserObserver.Begin {0}", gameId);

            _userContext.Data.PlayCount += 1;
            _userContext.Achivements.TryAchieved(AchievementKey.FirstPlay);
            _userContext.Achivements.TryProgress(AchievementKey.Play10Times, 1, 10);

            FlushUserContext();
        }

        void IGameUserObserver.End(long gameId, GameResult result)
        {
            _logger.TraceFormat("IGameUserObserver.End {0} {1}", gameId, result);

            switch (result)
            {
                case GameResult.Win:
                    _userContext.Data.WinCount += 1;
                    _userContext.Achivements.TryAchieved(AchievementKey.FirstWin);
                    _userContext.Achivements.TryProgress(AchievementKey.Win10Times, 1, 10);
                    break;

                case GameResult.Lose:
                    _userContext.Data.LoseCount += 1;
                    _userContext.Achivements.TryAchieved(AchievementKey.FirstLose);
                    _userContext.Achivements.TryProgress(AchievementKey.Lose10Times, 1, 10);
                    break;

                case GameResult.Draw:
                    _userContext.Data.DrawCount += 1;
                    _userContext.Achivements.TryAchieved(AchievementKey.FirstDraw);
                    _userContext.Achivements.TryProgress(AchievementKey.Draw10Times, 1, 10);
                    break;
            }

            FlushUserContext();
        }

        private void FlushUserContext()
        {
            // Notify changes to Client
            _userEventObserver.UserContextChange(_userContext.Tracker);

            // Notify change to MongoDB
            MongoDbStorage.UserContextMapper.SaveAsync(MongoDbStorage.Instance.UserCollection,
                                                       _userContext.Tracker, _id);

            // Clear changes
            _userContext.Tracker = new TrackableUserContextTracker();
        }
    }
}
