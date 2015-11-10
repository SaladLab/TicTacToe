using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain.Data;
using Domain.Interfaced;
using Akka.Interfaced.SlimSocket.Server;

namespace GameServer
{
    [Log]
    public class UserActor : InterfacedActor<UserActor>, IUser, IGameUserObserver
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private IActorRef _clientSession;
        private long _id;
        private TrackableUserContext _userContext;
        private UserEventObserver _userEventObserver;
        private Dictionary<long, GameRef> _joinedGameMap;

        public UserActor(ClusterNodeContext clusterContext, IActorRef clientSession,
                         long id, TrackableUserContext userContext, int observerId)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clusterContext = clusterContext;
            _clientSession = clientSession;
            _id = id;
            _userContext = userContext;
            _userEventObserver = new UserEventObserver(clientSession, observerId);
            _joinedGameMap = new Dictionary<long, GameRef>();
        }

        private void UnlinkAll()
        {
            foreach (var game in _joinedGameMap.Values)
                game.WithNoReply().Leave(_id);
            _joinedGameMap.Clear();

            _clusterContext.UserDirectory.WithNoReply().UnregisterUser(_id);
        }

        [MessageHandler]
        protected void OnMessage(ClientSessionMessage.BoundSessionTerminated message)
        {
            UnlinkAll();
            Context.Stop(Self);
        }

        Task IUser.RegisterPairing(int observerId)
        {
            var observer = new UserPairingObserver(_clientSession, observerId);
            return _clusterContext.GamePairMaker.RegisterPairing(_id, _userContext.Data.Name, observer);
        }

        Task IUser.UnregisterPairing()
        {
            return _clusterContext.GamePairMaker.UnregisterPairing(_id);
        }

        async Task<Tuple<int, int, GameInfo>> IUser.JoinGame(long gameId, int observerId)
        {
            if (_joinedGameMap.ContainsKey(gameId))
                throw new ResultException(ResultCodeType.NeedToBeOutOfGame);

            // Try to get game ref

            var gameRaw = await _clusterContext.GameDirectory.GetGame(gameId);
            if (gameRaw == null)
                throw new ResultException(ResultCodeType.GameNotFound);

            var game = ((GameRef)gameRaw).WithRequestWaiter(this);

            // Let's enter the game !

            var observer = new GameObserver(_clientSession, observerId);

            var observerIdForMe = IssueObserverId();
            var observerForMe  = new GameUserObserver(Self, observerIdForMe);
            AddObserver(observerIdForMe, this);

            var joinRet = await game.Join(_id, _userContext.Data.Name, observer, observerForMe);

            // Bind an player actor with client session

            var reply = await _clientSession.Ask<ClientSessionMessage.BindActorResponse>(
                new ClientSessionMessage.BindActorRequest
                {
                    Actor = game.Actor,
                    InterfaceType = typeof(IGamePlayer),
                    TagValue = _id
                });

            _joinedGameMap[gameId] = game;
            return Tuple.Create(reply.ActorId, joinRet.Item1, joinRet.Item2);
        }

        async Task IUser.LeaveGame(long gameId)
        {
            GameRef game;
            if (_joinedGameMap.TryGetValue(gameId, out game) == false)
                throw new ResultException(ResultCodeType.NeedToBeInGame);

            // Let's exit from the game !

            await game.Leave(_id);
            // TODO: Remove observer when leave

            // Unbind an player actor with client session

            _clientSession.Tell(
                new ClientSessionMessage.UnbindActorRequest { Actor = game.Actor });

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
