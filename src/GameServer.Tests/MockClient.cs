using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced.TestKit;
using Akka.TestKit;
using Domain;

namespace GameServer.Tests
{
    public class MockClient : IUserEventObserver
    {
        private ClusterNodeContext _clusterContext;
        private IActorRef _clientSessionActor;
        private TestActorBoundSession _clientSession;
        private UserLoginRef _userLogin;
        private long _userId;
        private UserRef _user;
        private UserEventObserver _userEventObserver;
        private TrackableUserContext _userContext;

        public IActorRef ClientSessionActor
        {
            get { return _clientSessionActor; }
        }

        public TestActorBoundSession ClientSession
        {
            get { return _clientSession; }
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

            var actorRef = new TestActorRef<TestActorBoundSession>(
                _clusterContext.System,
                Props.Create(() => new TestActorBoundSession(CreateInitialActor)));
            _clientSessionActor = actorRef;
            _clientSession = actorRef.UnderlyingActor;

            _userLogin = _clientSession.CreateRef<UserLoginRef>();
        }

        private Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context)
        {
            var actor = context.ActorOf(Props.Create(
                () => new UserLoginActor(_clusterContext, context.Self, new IPEndPoint(0, 0))));

            return new[] { Tuple.Create(actor, typeof(IUserLogin)) };
        }

        public async Task<LoginResult> LoginAsync(string id, string password)
        {
            if (_user != null)
                throw new InvalidOperationException("Already logined");

            _userEventObserver = (UserEventObserver)_clientSession.CreateObserver<IUserEventObserver>(this);

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
