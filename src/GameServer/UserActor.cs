using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class UserActor : InterfacedActor<UserActor>, IUser
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private IActorRef _clientSession;
        private string _id;
        private Dictionary<long, GameRef> _joinedGameMap;

        public UserActor(ClusterNodeContext clusterContext, IActorRef clientSession, string id, int observerId)
        {
            _logger = LogManager.GetLogger($"UserActor({id})");
            _clusterContext = clusterContext;
            _clientSession = clientSession;
            _id = id;
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
        protected void OnMessage(ClientSession.BoundSessionTerminatedMessage message)
        {
            UnlinkAll();
            Context.Stop(Self);
        }

        Task IUser.RegisterPairing(int observerId)
        {
            var observer = new UserPairingObserver(_clientSession, observerId);
            return _clusterContext.GamePairMaker.RegisterPairing(_id, observer);
        }

        Task IUser.UnregisterPairing()
        {
            return _clusterContext.GamePairMaker.UnregisterPairing(_id);
        }

        async Task<Tuple<int, GameInfo>> IUser.JoinGame(long gameId, int observerId)
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
            var info = await game.Join(_id, observer);

            // Bind an player actor with client session

            var reply = await _clientSession.Ask<ClientSession.BindActorResponseMessage>(
                new ClientSession.BindActorRequestMessage
                {
                    Actor = game.Actor,
                    InterfaceType = typeof(IGamePlayer),
                    TagValue = _id
                });

            _joinedGameMap[gameId] = game;
            return Tuple.Create(reply.ActorId, info);
        }

        async Task IUser.LeaveGame(long gameId)
        {
            GameRef game;
            if (_joinedGameMap.TryGetValue(gameId, out game) == false)
                throw new ResultException(ResultCodeType.NeedToBeInGame);

            // Let's exit from the game !

            await game.Leave(_id);

            // Unbind an player actor with client session

            _clientSession.Tell(
                new ClientSession.UnbindActorRequestMessage { Actor = game.Actor });

            _joinedGameMap.Remove(gameId);
        }
    }
}
