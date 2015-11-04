using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;
using System.Net;
using Domain.Data;
using Domain.Interfaced;
using MongoDB.Bson;
using MongoDB.Driver;
using TrackableData;
using TrackableData.MongoDB;

namespace GameServer
{
    [Log]
    public class UserLoginActor : InterfacedActor<UserLoginActor>, IUserLogin
    {
        private readonly ILog _logger;
        private readonly ClusterNodeContext _clusterContext;
        private readonly IActorRef _clientSession;

        public UserLoginActor(ClusterNodeContext clusterContext, IActorRef clientSession, EndPoint clientRemoteEndPoint)
        {
            _logger = LogManager.GetLogger($"UserLoginActor({clientRemoteEndPoint})");
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

        private class AccountUserMapInfo
        {
            public string Id;
            public long UserId;
        }

        async Task<LoginResult> IUserLogin.Login(string id, string password, int observerId)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            // Check account

            var accountId = await Authenticator.AuthenticateAsync(id, password);
            if (string.IsNullOrEmpty(accountId))
                throw new ResultException(ResultCodeType.LoginFailedIncorrectPassword);

            // Load user context from DB.
            // If no context, create new one.

            long userId;
            TrackableUserContext userContext;

            var accountUserMap = MongoDbStorage.Instance.Database.GetCollection<AccountUserMapInfo>("AccountUserMap");
            var userMap = await accountUserMap.Find(a => a.Id == accountId).FirstOrDefaultAsync();
            if (userMap != null)
            {
                userId = userMap.UserId;
                userContext = (TrackableUserContext)await MongoDbStorage.UserContextMapper.LoadAsync(
                    MongoDbStorage.Instance.UserCollection, 
                    userId);

                userContext.SetDefaultTracker();
                userContext.Data.LoginCount += 1;
                userContext.Data.LastLoginTime = DateTime.UtcNow;

                await MongoDbStorage.UserContextMapper.SaveAsync(MongoDbStorage.Instance.UserCollection,
                                                                 userContext.Tracker, userId);
                userContext.Tracker.Clear();
            }
            else
            {
                var created = CreateUser(accountId);
                userId = created.Item1;
                userContext = created.Item2;
                await MongoDbStorage.UserContextMapper.CreateAsync(
                    MongoDbStorage.Instance.UserCollection,
                    userContext, userId);
                await accountUserMap.InsertOneAsync(new AccountUserMapInfo { Id = accountId, UserId = userId });
                userContext.SetDefaultTracker();
            }

            // Make UserActor

            IActorRef user;
            try
            {
                user = Context.System.ActorOf(
                    Props.Create<UserActor>(_clusterContext, _clientSession, userId, userContext, observerId),
                    "user_" + userId);
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
                    await _clusterContext.UserDirectory.RegisterUser(userId, (IUser)userRef);
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

            return new LoginResult { UserId = userId, UserContext = userContext, UserActorBindId = reply.ActorId };
        }

        private Tuple<long, TrackableUserContext> CreateUser(string accountId)
        {
            var userId = UniqueInt64Id.GenerateNewId();
            var userContext = new TrackableUserContext
            {
                Data = new TrackableUserData
                {
                    Name = accountId.ToUpper(),
                    RegisterTime = DateTime.UtcNow,
                    LastLoginTime = DateTime.UtcNow,
                    LoginCount = 1,
                },
                Achivements = new TrackableDictionary<int, UserAchievement>()
            };
            return Tuple.Create(userId, userContext);
        }
    }
}
