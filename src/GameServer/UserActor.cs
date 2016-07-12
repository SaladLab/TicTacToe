using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Akka.Interfaced.SlimServer;
using Common.Logging;
using Domain;
using TrackableData;

namespace GameServer
{
    [Log]
    [ResponsiveException(typeof(ResultException))]
    public class UserActor : InterfacedActor, IActorBoundChannelObserver, IUserInitiator, IUser, IGameUserObserver
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private ActorBoundChannelRef _channel;
        private long _id;
        private TrackableUserContext _userContext;
        private TrackableUserContextTracker _userContextSaveTracker;
        private UserEventObserver _userEventObserver;
        private Dictionary<long, GameRef> _joinedGameMap = new Dictionary<long, GameRef>();

        public UserActor(ClusterNodeContext clusterContext, long id)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clusterContext = clusterContext;
            _id = id;
        }

        protected override Task OnGracefulStop()
        {
            return SaveUserContextChangeToDb();
        }

        protected override void PostStop()
        {
            UnlinkAll();
            base.PostStop();
        }

        private void UnlinkAll()
        {
            foreach (var game in _joinedGameMap.Values)
                game.WithNoReply().Leave(_id);
            _joinedGameMap.Clear();
        }

        private void FlushUserContext()
        {
            if (_userEventObserver != null)
                _userEventObserver.UserContextChange(_userContext.Tracker);

            _userContext.Tracker.ApplyTo(_userContextSaveTracker);
            _userContext.Tracker = new TrackableUserContextTracker();
        }

        private Task SaveUserContextChangeToDb()
        {
            return (_userContextSaveTracker != null && _userContextSaveTracker.HasChange)
                ? MongoDbStorage.UserContextMapper.SaveAsync(MongoDbStorage.Instance.UserCollection, _userContextSaveTracker, _id)
                : Task.CompletedTask;
        }

        void IActorBoundChannelObserver.ChannelOpen(IActorBoundChannel channel, object tag)
        {
            _channel = (ActorBoundChannelRef)channel;
        }

        void IActorBoundChannelObserver.ChannelOpenTimeout(object tag)
        {
            Self.Tell(InterfacedPoisonPill.Instance);
        }

        void IActorBoundChannelObserver.ChannelClose(IActorBoundChannel channel, object tag)
        {
            _channel = null;
        }

        async Task<TrackableUserContext> IUserInitiator.Create(IUserEventObserver observer, string name)
        {
            // create context

            var userContext = new TrackableUserContext
            {
                Data = new TrackableUserData
                {
                    Name = name,
                    RegisterTime = DateTime.UtcNow,
                    LastLoginTime = DateTime.UtcNow,
                    LoginCount = 1,
                },
                Achivements = new TrackableDictionary<int, UserAchievement>()
            };

            await MongoDbStorage.UserContextMapper.CreateAsync(
                MongoDbStorage.Instance.UserCollection,
                userContext, _id);

            await OnUserInitiated(userContext, observer);
            return userContext;
        }

        async Task<TrackableUserContext> IUserInitiator.Load(IUserEventObserver observer)
        {
            // load context

            var userContext = (TrackableUserContext)await MongoDbStorage.UserContextMapper.LoadAsync(
                MongoDbStorage.Instance.UserCollection,
                _id);
            if (userContext == null)
                throw new ResultException(ResultCodeType.UserNeedToBeCreated);

            await OnUserInitiated(userContext, observer);

            // user post-login handler

            userContext.SetDefaultTracker();
            userContext.Data.LoginCount += 1;
            userContext.Data.LastLoginTime = DateTime.UtcNow;
            userContext.Tracker.ApplyTo(_userContextSaveTracker);

            return userContext;
        }

        private async Task OnUserInitiated(TrackableUserContext userContext, IUserEventObserver observer)
        {
            _userContext = userContext.Clone();
            _userContext.SetDefaultTracker();
            _userContextSaveTracker = new TrackableUserContextTracker();

            _userEventObserver = (UserEventObserver)observer;

            _channel.WithNoReply().UnbindType(Self, new[] { typeof(IUserInitiator) });
            await _channel.BindType(Self, new TaggedType[] { typeof(IUser) });
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

            IActorRef gameRef;
            try
            {
                var reply = await _clusterContext.GameTable.GetOrCreate(gameId, null);
                gameRef = reply.Actor;
            }
            catch (Exception e)
            {
                _logger.Warn($"Failed in querying game from GameTable. (Id={gameId})", e);
                throw new ResultException(ResultCodeType.InternalError);
            }

            if (gameRef == null)
                throw new ResultException(ResultCodeType.GameNotFound);

            var game = gameRef.Cast<GameRef>().WithRequestWaiter(this);

            // Let's enter the game !

            var observerForMe = CreateObserver<IGameUserObserver>();
            var joinRet = await game.Join(_id, _userContext.Data.Name, observer, observerForMe);

            IRequestTarget boundTarget = null;
            try
            {
                boundTarget = await _channel.BindActorOrOpenChannel(
                    game.CastToIActorRef(), new[] { new TaggedType(typeof(IGamePlayer), _id) },
                    ActorBindingFlags.OpenThenNotification | ActorBindingFlags.CloseThenNotification,
                    "GameGateway", _id);
            }
            catch (Exception e)
            {
                _logger.Error($"BindActorOrOpenChannel error (Id={gameId})", e);
            }

            if (boundTarget == null)
            {
                await game.Leave(_id);
                _logger.Error($"Failed in binding GamePlayer");
                throw new ResultException(ResultCodeType.InternalError);
            }

            _joinedGameMap[gameId] = game;
            return Tuple.Create((IGamePlayer)boundTarget.Cast<GamePlayerRef>(), joinRet.Item1, joinRet.Item2);
        }

        async Task IUser.LeaveGame(long gameId)
        {
            GameRef game;
            if (_joinedGameMap.TryGetValue(gameId, out game) == false)
                throw new ResultException(ResultCodeType.NeedToBeInGame);

            // Let's leave from the game !

            await game.Leave(_id);

            // Unbind an game actor from channel

            _channel.WithNoReply().UnbindActor(game.CastToIActorRef());

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
    }
}
