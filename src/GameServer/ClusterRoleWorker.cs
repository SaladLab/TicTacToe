using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Utility;
using Akka.Interfaced;
using Akka.Interfaced.SlimServer;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain;

namespace GameServer
{
    public abstract class ClusterRoleWorker
    {
        public ClusterNodeContext Context { get; }

        public ClusterRoleWorker(ClusterNodeContext context)
        {
            Context = context;
        }

        public abstract Task Start();
        public abstract Task Stop();
    }

    public class UserTableWorker : ClusterRoleWorker
    {
        private IActorRef _userTable;

        public UserTableWorker(ClusterNodeContext context)
            : base(context)
        {
        }

        public override Task Start()
        {
            _userTable = Context.System.ActorOf(
                Props.Create(() => new DistributedActorTable<long>("User", Context.ClusterActorDiscovery, null, null)),
                "UserTable");
            return Task.FromResult(true);
        }

        public override async Task Stop()
        {
            await _userTable.GracefulStop(
                TimeSpan.FromMinutes(1),
                new DistributedActorTableMessage<long>.GracefulStop(InterfacedPoisonPill.Instance));
        }
    }

    public class UserWorker : ClusterRoleWorker
    {
        private IActorRef _userContainer;
        private ChannelType _channelType;
        private IPEndPoint _listenEndPoint;
        private GatewayRef _gateway;

        public UserWorker(ClusterNodeContext context, ChannelType channelType, IPEndPoint listenEndPoint)
            : base(context)
        {
            _channelType = channelType;
            _listenEndPoint = listenEndPoint;
        }

        public override async Task Start()
        {
            // create UserTableContainer

            _userContainer = Context.System.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>("User", Context.ClusterActorDiscovery, null, null)),
                "UserTableContainer");
            Context.UserTableContainer = _userContainer;

            // create gateway for users to connect to

            if (_listenEndPoint.Port != 0)
            {
                var serializer = PacketSerializer.CreatePacketSerializer();

                var initiator = new GatewayInitiator
                {
                    ListenEndPoint = _listenEndPoint,
                    GatewayLogger = LogManager.GetLogger($"Gateway({_channelType})"),
                    CreateChannelLogger = (ep, _) => LogManager.GetLogger($"Channel({ep}"),
                    ConnectionSettings = new TcpConnectionSettings { PacketSerializer = serializer },
                    PacketSerializer = serializer,
                    CreateInitialActors = (context, connection) => new[]
                    {
                    Tuple.Create(
                        context.ActorOf(Props.Create(() =>
                            new UserLoginActor(Context, context.Self.Cast<ActorBoundChannelRef>(), GatewayInitiator.GetRemoteEndPoint(connection)))),
                        new TaggedType[] { typeof(IUserLogin) },
                        (ActorBindingFlags)0)
                }
                };

                var gateway = (_channelType == ChannelType.Tcp)
                    ? Context.System.ActorOf(Props.Create(() => new TcpGateway(initiator)), "TcpGateway").Cast<GatewayRef>()
                    : Context.System.ActorOf(Props.Create(() => new UdpGateway(initiator)), "UdpGateway").Cast<GatewayRef>();

                await gateway.Start();

                _gateway = gateway;
            }
        }

        public override async Task Stop()
        {
            if (_gateway != null)
            {
                await _gateway.Stop();
                await _gateway.CastToIActorRef().GracefulStop(TimeSpan.FromSeconds(10), new Identify(0));
            }

            await _userContainer.GracefulStop(TimeSpan.FromSeconds(10), PoisonPill.Instance);
        }
    }

    public class GamePairMakerWorker : ClusterRoleWorker
    {
        private IActorRef _pairMaker;

        public GamePairMakerWorker(ClusterNodeContext context)
            : base(context)
        {
        }

        public override Task Start()
        {
            _pairMaker = Context.System.ActorOf(
                Props.Create(() => new GamePairMakerActor(Context)),
                "GamePairMakerActor");
            return Task.FromResult(true);
        }

        public override async Task Stop()
        {
            await _pairMaker.GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance);
        }
    }

    public class GameTableWorker : ClusterRoleWorker
    {
        private IActorRef _gameTable;

        public GameTableWorker(ClusterNodeContext context)
            : base(context)
        {
        }

        public override Task Start()
        {
            _gameTable = Context.System.ActorOf(
                Props.Create(() => new DistributedActorTable<long>("Game", Context.ClusterActorDiscovery, typeof(IncrementalIntegerIdGenerator), null)),
                "GameTable");
            return Task.FromResult(true);
        }

        public override async Task Stop()
        {
            await _gameTable.GracefulStop(
                TimeSpan.FromMinutes(1),
                new DistributedActorTableMessage<long>.GracefulStop(InterfacedPoisonPill.Instance));
            _gameTable = null;
        }
    }

    public class GameWorker : ClusterRoleWorker
    {
        private IActorRef _gameContainer;

        public GameWorker(ClusterNodeContext context)
            : base(context)
        {
        }

        public override Task Start()
        {
            // create GameTableContainer

            _gameContainer = Context.System.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>(
                    "Game", Context.ClusterActorDiscovery, typeof(GameActorFactory), new object[] { Context })),
                "GameTableContainer");
            Context.GameTableContainer = _gameContainer;

            return Task.FromResult(true);
        }

        public override async Task Stop()
        {
            await _gameContainer.GracefulStop(TimeSpan.FromSeconds(10), PoisonPill.Instance);
        }
    }

    public class GameActorFactory : IActorFactory
    {
        private ClusterNodeContext _clusterContext;

        public void Initialize(object[] args)
        {
            _clusterContext = (ClusterNodeContext)args[0];
        }

        public IActorRef CreateActor(IActorRefFactory actorRefFactory, object id, object[] args)
        {
            var param = (CreateGameParam)args[0];

            var gameActor = actorRefFactory.ActorOf(Props.Create(
                () => new GameActor(_clusterContext, (long)id, param)));

            if (param.WithBot)
            {
                actorRefFactory.ActorOf(Props.Create(
                    () => new GameBotActor(_clusterContext, gameActor.Cast<GameRef>(), 0, "bot")));
            }

            return gameActor;
        }
    }
}
