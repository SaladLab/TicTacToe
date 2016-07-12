using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Akka.Interfaced.SlimServer;
using Akka.Interfaced.TestKit;
using Akka.TestKit;
using Domain;

namespace GameServer.Tests
{
    public class MockClient : IUserEventObserver
    {
        private ClusterNodeContext _clusterContext;
        private UserEventObserver _userEventObserver;

        public TestActorBoundChannel Channel { get; private set; }
        public ActorBoundChannelRef ChannelRef { get; private set; }
        public string LoginId { get; private set; }
        public UserLoginRef UserLogin { get; private set; }
        public long UserId { get; private set; }
        public UserInitiatorRef UserInitiator { get; private set; }
        public UserRef User { get; private set; }
        public TrackableUserContext UserContext { get; private set; }

        public MockClient(ClusterNodeContext clusterContex)
        {
            _clusterContext = clusterContex;

            var channel = new TestActorRef<TestActorBoundChannel>(
                _clusterContext.System,
                 Props.Create(() => new TestActorBoundChannel(CreateInitialActor)));
            Channel = channel.UnderlyingActor;
            ChannelRef = channel.Cast<ActorBoundChannelRef>();

            UserLogin = Channel.CreateRef<UserLoginRef>();
        }

        private Tuple<IActorRef, TaggedType[], ActorBindingFlags>[] CreateInitialActor(IActorContext context) =>
            new[]
            {
                Tuple.Create(
                    context.ActorOf(Props.Create(() =>
                        new UserLoginActor(_clusterContext, context.Self.Cast<ActorBoundChannelRef>(), new IPEndPoint(IPAddress.None, 0)))),
                    new TaggedType[] { typeof(IUserLogin) },
                    (ActorBindingFlags)0)
            };

        public async Task LoginAsync(string id, string password)
        {
            if (LoginId != null)
                throw new InvalidOperationException("Already logined");

            var loginRet = await UserLogin.Login(id, password);
            LoginId = id;
            UserId = loginRet.Item1;
            UserInitiator = (UserInitiatorRef)loginRet.Item2;
        }

        public async Task PrepareUserAsync(string id, string password, string userName = null)
        {
            if (User != null)
                throw new InvalidOperationException("Already user prepared!");

            if (UserInitiator == null)
                await LoginAsync(id, password);

            _userEventObserver = (UserEventObserver)Channel.CreateObserver<IUserEventObserver>(this);

            try
            {
                UserContext = await UserInitiator.Load(_userEventObserver);
            }
            catch (ResultException e)
            {
                if (e.ResultCode == ResultCodeType.UserNeedToBeCreated)
                    UserContext = await UserInitiator.Create(_userEventObserver, userName ?? id.ToUpper());
                else
                    throw;
            }

            User = UserInitiator.Cast<UserRef>();
        }

        void IUserEventObserver.UserContextChange(TrackableUserContextTracker userContextTracker)
        {
            // this method is called by a worker thread of TestActorBoundSession actor
            // which is not same with with a test thread but invocation is serialized.
            // so if you access _userContext carefully, it could be safe :)
            userContextTracker.ApplyTo(UserContext);
        }
    }
}
