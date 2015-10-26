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
    public class UserActor : InterfacedActor<UserActor>, IUser// , IUserMessasing
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private IActorRef _clientSession;
        private string _id;
        // private UserEventObserver _eventObserver;
        // private Dictionary<string, RoomRef> _enteredRoomMap;

        public UserActor(ClusterNodeContext clusterContext, IActorRef clientSession, string id, int observerId)
        {
            _logger = LogManager.GetLogger(string.Format("UserActor({0})", id));
            _clusterContext = clusterContext;
            _clientSession = clientSession;
            _id = id;
            //_eventObserver = new UserEventObserver(_clientSession, observerId);
            //_enteredRoomMap = new Dictionary<string, RoomRef>();
        }

        protected override void OnReceiveUnhandled(object message)
        {
            if (message is ClientSession.BoundSessionTerminatedMessage)
            {
                UnlinkAll();
                Context.Stop(Self);
            }
            else
            {
                base.OnReceiveUnhandled(message);
            }
        }

        private void UnlinkAll()
        {
            //foreach (var room in _enteredRoomMap.Values)
            //    room.WithNoReply().Exit(_id);
            //_enteredRoomMap.Clear();

            _clusterContext.UserDirectory.WithNoReply().UnregisterUser(_id);
        }

        Task<string> IUser.GetId()
        {
            return Task.FromResult(_id);
        }

        /*
        async Task<List<string>> IUser.GetRoomList()
        {
            var roomList = await _clusterContext.RoomDirectory.GetRoomList();
            return roomList;
        }

        async Task<Tuple<int, RoomInfo>> IUser.EnterRoom(string name, int observerId)
        {
            if (_enteredRoomMap.ContainsKey(name))
                throw new ResultException(ResultCodeType.NeedToBeOutOfRoom);

            // Try to get room ref

            var roomRaw = await _clusterContext.RoomDirectory.GetOrCreateRoom(name);
            if (roomRaw == null)
                throw new ResultException(ResultCodeType.RoomRemoved);

            var room = ((RoomRef)roomRaw).WithRequestWaiter(this);

            // Let's enter the room !

            var observer = new RoomObserver(_clientSession, observerId);
            var info = await room.Enter(_id, observer);

            // Bind an occupant actor with client session

            var reply = await _clientSession.Ask<ClientSession.BindActorResponseMessage>(
                new ClientSession.BindActorRequestMessage
                {
                    Actor = room.Actor,
                    InterfaceType = typeof(IOccupant),
                    TagValue = _id
                });

            _enteredRoomMap[name] = room;
            return Tuple.Create(reply.ActorId, info);
        }

        async Task IUser.ExitFromRoom(string name)
        {
            RoomRef room;
            if (_enteredRoomMap.TryGetValue(name, out room) == false)
                throw new ResultException(ResultCodeType.NeedToBeInRoom);

            // Let's exit from the room !

            await room.Exit(_id);

            // Unbind an occupant actor with client session

            _clientSession.Tell(
                new ClientSession.UnbindActorRequestMessage { Actor = room.Actor });

            _enteredRoomMap.Remove(name);
        }

        async Task IUser.Whisper(string targetUserId, string message)
        {
            if (targetUserId == _id)
                throw new ResultException(ResultCodeType.UserNotMyself);

            var targetUser = await _clusterContext.UserDirectory.GetUser(targetUserId);
            if (targetUser == null)
                throw new ResultException(ResultCodeType.UserNotOnline);

            var chatItem = new ChatItem
            {
                UserId = _id,
                Time = DateTime.UtcNow,
                Message = message
            };

            // TODO: not a good way.. is there a type-safe way?
            var targetUserMessaging = new UserMessasingRef(((UserRef)targetUser).Actor, null, null);
            targetUserMessaging.WithNoReply().Whisper(chatItem);
        }

        Task IUserMessasing.Whisper(ChatItem chatItem)
        {
            _eventObserver.Whisper(chatItem);
            return Task.FromResult(0);
        }

        Task IUserMessasing.Invite(string invitorUserId, string roomName)
        {
            _eventObserver.Invite(invitorUserId, roomName);
            return Task.FromResult(0);
        }
        */
    }
}
