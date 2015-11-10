using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class UserDirectoryActor : InterfacedActor<UserDirectoryActor>, IUserDirectory
    {
        private ILog _logger = LogManager.GetLogger("UserDirectoryActor");
        private readonly ClusterNodeContext _clusterContext;
        private readonly Dictionary<long, IUser> _userTable;

        public UserDirectoryActor(ClusterNodeContext clusterContext)
        {
            _clusterContext = clusterContext;

            _clusterContext.ClusterActorDiscovery.Tell(
                new ClusterActorDiscoveryMessages.RegisterActor(Self, nameof(IUserDirectory)),
                Self);

            _userTable = new Dictionary<long, IUser>();
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

        Task IUserDirectory.RegisterUser(long userId, IUser user)
        {
            _userTable.Add(userId, user);
            return Task.FromResult(true);
        }

        Task IUserDirectory.UnregisterUser(long userId)
        {
            _userTable.Remove(userId);
            return Task.FromResult(true);
        }

        Task<IUser> IUserDirectory.GetUser(long userId)
        {
            IUser user;
            return Task.FromResult(_userTable.TryGetValue(userId, out user) ? user : null);
        }
    }
}
