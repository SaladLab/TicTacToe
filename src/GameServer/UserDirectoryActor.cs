using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using Domain.Interfaced;

namespace GameServer
{
    public class UserDirectoryActor : InterfacedActor<UserDirectoryActor>, IUserDirectory
    {
        private ClusterNodeContext _clusterContext;
        private Dictionary<string, IUser> _userTable;

        public UserDirectoryActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            _clusterContext.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.ActorUp { Actor = Self, Type = typeof(IUserDirectory) },
                Self);

            _userTable = new Dictionary<string, IUser>();
        }

        protected override void OnReceiveUnhandled(object message)
        {
            var shutdownMessage = message as ShutdownMessage;
            if (shutdownMessage != null)
            {
                Context.Stop(Self);
                return;
            }

            base.OnReceiveUnhandled(message);
        }

        Task IUserDirectory.RegisterUser(string userId, IUser user)
        {
            _userTable.Add(userId, user);
            return Task.FromResult(true);
        }

        Task IUserDirectory.UnregisterUser(string userId)
        {
            _userTable.Remove(userId);
            return Task.FromResult(true);
        }

        Task<IUser> IUserDirectory.GetUser(string userId)
        {
            IUser user;
            return Task.FromResult(_userTable.TryGetValue(userId, out user) ? user : null);
        }
    }
}
