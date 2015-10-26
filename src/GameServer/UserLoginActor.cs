using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;
using System.Net;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class UserLoginActor : InterfacedActor<UserLoginActor>, IUserLogin
    {
        private ILog _logger;
        private ClusterNodeContext _clusterContext;
        private IActorRef _clientSession;

        public UserLoginActor(ClusterNodeContext clusterContext, IActorRef clientSession, EndPoint clientRemoteEndPoint)
        {
            _logger = LogManager.GetLogger(string.Format("UserLoginActor({0})", clientRemoteEndPoint));
            _clusterContext = clusterContext;
            _clientSession = clientSession;
        }

        protected override void OnReceiveUnhandled(object message)
        {
            if (message is ClientSession.BoundSessionTerminatedMessage)
            {
                Context.Stop(Self);
            }
            else
            {
                base.OnReceiveUnhandled(message);
            }
        }

        async Task<int> IUserLogin.Login(string id, string password, int observerId)
        {
            //Contract.Requires<ArgumentNullException>(id != null);
            //Contract.Requires<ArgumentNullException>(password != null);

            // Check password

            if (await Authenticator.AuthenticateAsync(id, password) == false)
                throw new ResultException(ResultCodeType.LoginFailedIncorrectPassword);

            // Make UserActor

            IActorRef user = null;
            try
            {
                user = Context.System.ActorOf(
                    Props.Create<UserActor>(_clusterContext, _clientSession, id, observerId),
                    "user_" + id);
            }
            catch (Exception)
            {
                throw new ResultException(ResultCodeType.LoginFailedAlreadyConnected);
            }

            // Register User in UserDirectory

            var userRef = new UserRef(user);
            var registered = false;
            for (int i=0; i<10; i++)
            {
                try
                {
                    await _clusterContext.UserDirectory.RegisterUser(id, (IUser)userRef);
                    registered = true;
                    break;
                }
                catch (Exception)
                {
                    // TODO: Send Disconnect Message To Already Registered User.
                }

                await Task.Delay(200);
            }
            if (registered == false)
            {
                user.Tell(PoisonPill.Instance);
                throw new ResultException(ResultCodeType.LoginFailedAlreadyConnected);
            }

            // Bind user actor with client session, which makes client to communicate with this actor.

            var reply = await _clientSession.Ask<ClientSession.BindActorResponseMessage>(
                new ClientSession.BindActorRequestMessage { Actor = user, InterfaceType = typeof(IUser) });

            return reply.ActorId;
        }
    }
}
