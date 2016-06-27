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
        private TestActorBoundChannel _channel;
        private ActorBoundChannelRef _channelRef;
        private UserLoginRef _userLogin;
        private long _userId;
        private UserRef _user;
        private UserEventObserver _userEventObserver;
        private TrackableUserContext _userContext;

        public TestActorBoundChannel Channel
        {
            get { return _channel; }
        }

        public ActorBoundChannelRef ChannelRef
        {
            get { return _channelRef; }
        }

        public UserLoginRef UserLogin
        {
            get { return _userLogin; }
        }

        public long UserId
        {
            get { return _userId; }
        }

        public UserRef User
        {
            get { return _user; }
        }

        public TrackableUserContext UserContext
        {
            get { return _userContext; }
        }

        public MockClient(ClusterNodeContext clusterContex)
        {
            _clusterContext = clusterContex;

            var channel = new TestActorRef<TestActorBoundChannel>(
                _clusterContext.System,
                 Props.Create(() => new TestActorBoundChannel(CreateInitialActor)));
            _channel = channel.UnderlyingActor;
            _channelRef = channel.Cast<ActorBoundChannelRef>();

            _userLogin = _channel.CreateRef<UserLoginRef>();
        }

        private Tuple<IActorRef, TaggedType[], ActorBindingFlags>[] CreateInitialActor(IActorContext context) =>
            new[]
            {
                Tuple.Create(
                    context.ActorOf(Props.Create(() =>
                        new UserLoginActor(_clusterContext, context.Self.Cast<ActorBoundChannelRef>(), new IPEndPoint(0, 0)))),
                    new TaggedType[] { typeof(IUserLogin) },
                    (ActorBindingFlags)0)
            };

        public async Task<LoginResult> LoginAsync(string id, string password)
        {
            if (_user != null)
                throw new InvalidOperationException("Already logined");

            _userEventObserver = (UserEventObserver)_channel.CreateObserver<IUserEventObserver>(this);

            var ret = await _userLogin.Login(id, password, _userEventObserver);
            _userId = ret.UserId;
            _user = (UserRef)ret.User;
            _userContext = new TrackableUserContext();
            return ret;
        }

        void IUserEventObserver.UserContextChange(TrackableUserContextTracker userContextTracker)
        {
            // this method is called by a worker thread of TestActorBoundSession actor
            // which is not same with with a test thread but invocation is serialized.
            // so if you access _userContext carefully, it could be safe :)
            userContextTracker.ApplyTo(_userContext);
        }
    }
}
