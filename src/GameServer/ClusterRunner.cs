using System;
using System.Collections.Generic;
using System.Net;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Utility;
using Akka.Configuration;
using Akka.Interfaced;
using Common.Logging;
using Domain.Interfaced;
using Akka.Interfaced.SlimSocket.Server;
using Akka.Interfaced.SlimSocket.Base;
using ProtoBuf.Meta;
using TypeAlias;
using System.Net.Sockets;
using Akka.Interfaced.ProtobufSerializer;

namespace GameServer
{
    public class ClusterRunner
    {
        private readonly Config _commonConfig;

        private readonly List<Tuple<ActorSystem, List<IActorRef>>> _nodes =
            new List<Tuple<ActorSystem, List<IActorRef>>>();

        public ClusterRunner(Config commonConfig)
        {
            _commonConfig = commonConfig;
        }

        public void LaunchNode(int port, int clientPort, params string[] roles)
        {
            var config = _commonConfig
                .WithFallback("akka.remote.helios.tcp.port = " + port)
                .WithFallback("akka.cluster.roles = " + "[" + string.Join(",", roles) + "]");

            var system = ActorSystem.Create("GameCluster", config);

            DeadRequestProcessingActor.Install(system);

            var cluster = Cluster.Get(system);
            var context = new ClusterNodeContext { System = system };

            context.ClusterActorDiscovery =
                system.ActorOf(Props.Create(() => new ClusterActorDiscovery(cluster)), "ClusterActorDiscovery");
            context.ClusterNodeContextUpdater =
                system.ActorOf(Props.Create(() => new ClusterNodeContextUpdater(context)), "ClusterNodeContextUpdater");

            var actors = new List<IActorRef>();
            foreach (var role in roles)
            {
                switch (role)
                {
                    case "user-table":
                        actors.AddRange(InitUserTable(context));
                        break;

                    case "user":
                        actors.AddRange(InitUser(context, clientPort));
                        break;

                    case "game-pair-maker":
                        actors.AddRange(InitGamePairMaker(context));
                        break;

                    case "game-table":
                        actors.AddRange(InitGameTable(context));
                        break;

                    case "game":
                        actors.AddRange(InitGame(context));
                        break;

                    default:
                        throw new InvalidOperationException("Invalid role: " + role);
                }
            }

            _nodes.Add(Tuple.Create(system, actors));
        }

        public void Shutdown()
        {
            _nodes.Reverse();
            foreach (var cluster in _nodes)
            {
                // stop all root-actors in reverse

                var rootActors = cluster.Item2;
                rootActors.Reverse();
                foreach (var actor in rootActors)
                    actor.GracefulStop(TimeSpan.FromSeconds(30), new ShutdownMessage()).Wait();

                // stop system

                cluster.Item1.Shutdown();
            }
        }

        private IActorRef[] InitUserTable(ClusterNodeContext context)
        {
            return new[]
            {
                context.System.ActorOf(
                    Props.Create(() => new DistributedActorTable<long>(
                                           "User", context.ClusterActorDiscovery, null, null)),
                    "UserTable")
            };
        }

        private IActorRef[] InitUser(ClusterNodeContext context, int clientPort)
        {
            var container = context.System.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>(
                                       "User", context.ClusterActorDiscovery, null, null)),
                "UserTableContainer");
            context.UserTableContainer = container;

            var userSystem = new UserClusterSystem(context);
            var gateway = userSystem.Start(clientPort);

            return new[] { container, gateway };
        }

        private IActorRef[] InitGameTable(ClusterNodeContext context)
        {
            return new[]
            {
                context.System.ActorOf(
                    Props.Create(() => new DistributedActorTable<long>(
                                           "Game", context.ClusterActorDiscovery, 
                                           typeof(IncrementalIntegerIdGenerator), null)),
                    "GameTable")
            };
        }

        private IActorRef[] InitGame(ClusterNodeContext context)
        {
            var container = context.System.ActorOf(
                Props.Create(() => new DistributedActorTableContainer<long>(
                                       "Game", context.ClusterActorDiscovery,
                                       typeof(GameActorFactory), new object[] { context })),
                "GameTableContainer");
            context.GameTableContainer = container;

            return new[] { container };
        }

        private IActorRef[] InitGamePairMaker(ClusterNodeContext context)
        {
            return new[]
            {
                context.System.ActorOf(
                    Props.Create(() => new GamePairMakerActor(context)),
                    "GamePairMakerActor")
            };
        }
    }

    public class UserClusterSystem
    {
        private readonly ClusterNodeContext _context;
        private readonly IActorRef _userTableContainer;
        private readonly TcpConnectionSettings _tcpConnectionSettings;

        public UserClusterSystem(ClusterNodeContext context)
        {
            _context = context;

            var typeModel = TypeModel.Create();
            AutoSurrogate.Register(typeModel);
            _tcpConnectionSettings = new TcpConnectionSettings
            {
                PacketSerializer = new PacketSerializer(
                    new PacketSerializerBase.Data(
                        new ProtoBufMessageSerializer(typeModel),
                        new TypeAliasTable()))
            };
        }

        public IActorRef Start(int port)
        {
            var logger = LogManager.GetLogger("ClientGateway");
            var clientGateway = _context.System.ActorOf(Props.Create(() => new ClientGateway(logger, CreateSession)));
            clientGateway.Tell(new ClientGatewayMessage.Start(new IPEndPoint(IPAddress.Any, port)));
            return clientGateway;
        }

        private IActorRef CreateSession(IActorContext context, Socket socket)
        {
            var logger = LogManager.GetLogger($"Client({socket.RemoteEndPoint})");
            return context.ActorOf(Props.Create(
                () => new ClientSession(logger, socket, _tcpConnectionSettings, CreateInitialActor)));
        }

        private Tuple<IActorRef, Type>[] CreateInitialActor(IActorContext context, Socket socket)
        {
            return new[]
            {
                    Tuple.Create(
                        context.ActorOf(Props.Create(
                            () => new UserLoginActor(_context, context.Self, socket.RemoteEndPoint))),
                        typeof(IUserLogin))
                };
        }
    };

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
                    () => new GameBotActor(_clusterContext, new GameRef(gameActor), 0, "bot")));
            }

            return gameActor;
        }
    }
}
