using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.LogFilter;
using Akka.Interfaced.SlimServer;
using Common.Logging;
using Domain;

namespace GameServer
{
    [Log]
    [ResponsiveException(typeof(ResultException))]
    public class UserLoginActor : InterfacedActor, IUserLogin
    {
        private readonly ILog _logger;
        private readonly ClusterNodeContext _clusterContext;
        private readonly ActorBoundChannelRef _channel;

        public UserLoginActor(ClusterNodeContext clusterContext, ActorBoundChannelRef channel, EndPoint clientRemoteEndPoint)
        {
            _logger = LogManager.GetLogger($"UserLoginActor({clientRemoteEndPoint})");
            _clusterContext = clusterContext;
            _channel = channel;
        }

        async Task<Tuple<long, IUserInitiator>> IUserLogin.Login(string id, string password)
        {
            if (id == null)
                throw new ResultException(ResultCodeType.ArgumentError, nameof(id));
            if (password == null)
                throw new ResultException(ResultCodeType.ArgumentError, nameof(password));

            // verify crendential
            var userId = await Authenticator.AuthenticateAsync(id, password);
            if (userId <= 0)
                throw new ResultException(ResultCodeType.LoginCredentialError);

            // try to create user actor with user-id
            var user = await _clusterContext.UserTable.WithTimeout(TimeSpan.FromSeconds(30)).GetOrCreate(userId, null);
            if (user.Actor == null)
                throw new ResultException(ResultCodeType.InternalError);
            if (user.Created == false)
                throw new ResultException(ResultCodeType.LoginAlreadyLoginedError);

            // bound actor to this channel or new channel on user gateway
            IRequestTarget boundTarget;
            try
            {
                boundTarget = await _channel.BindActorOrOpenChannel(
                    user.Actor, new TaggedType[] { typeof(IUserInitiator) },
                    ActorBindingFlags.OpenThenNotification | ActorBindingFlags.CloseThenStop | ActorBindingFlags.StopThenCloseChannel,
                    "UserGateway", null);
            }
            catch (Exception e)
            {
                _logger.Error($"BindActorOrOpenChannel error (UserId={userId})", e);
                user.Actor.Tell(InterfacedPoisonPill.Instance);
                throw new ResultException(ResultCodeType.InternalError);
            }

            // once login done, stop this
            Self.Tell(InterfacedPoisonPill.Instance);

            return Tuple.Create(userId, (IUserInitiator)boundTarget.Cast<UserInitiatorRef>());
        }
    }
}
