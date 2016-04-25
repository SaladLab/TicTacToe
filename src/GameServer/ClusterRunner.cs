using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Utility;
using Akka.Configuration;
using Akka.Interfaced;
using Akka.Interfaced.ProtobufSerializer;
using Akka.Interfaced.SlimSocket.Base;
using Akka.Interfaced.SlimSocket.Server;
using Common.Logging;
using Domain.Interfaced;
using ProtoBuf.Meta;
using TypeAlias;

namespace GameServer
{
    public class ClusterRunner
    {
        private readonly Config _commonConfig;

        public class Node
        {
            public ActorSystem System;

            public class RoleActor
            {
                public string Role { get; }
                public IActorRef[] Actors { get; }

                public RoleActor(string role, IActorRef[] actors)
                {
                    Role = role;
                    Actors = actors;
                }
            }

            public RoleActor[] RoleActors;
        }

        private readonly List<Node> _nodes = new List<Node>();

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

            var roleActors = new List<Node.RoleActor>();
            foreach (var role in roles)
            {
                switch (role)
                {
                    case "user-table":
                        roleActors.Add(new Node.RoleActor(role, InitUserTable(context)));
                        break;

                    case "user":
                        roleActors.Add(new Node.RoleActor(role, InitUser(context, clientPort)));
                        break;

                    case "game-pair-maker":
                        roleActors.Add(new Node.RoleActor(role, InitGamePairMaker(context)));
                        break;

                    case "game-table":
                        roleActors.Add(new Node.RoleActor(role, InitGameTable(context)));
                        break;

                    case "game":
                        roleActors.Add(new Node.RoleActor(role, InitGame(context)));
                        break;

                    default:
                        throw new InvalidOperationException("Invalid role: " + role);
                }
            }

            _nodes.Add(new Node
            {
                System = system,
                RoleActors = roleActors.ToArray()
            });
        }

        public void Shutdown()
        {
            Console.WriteLine("Shutdown: User Listen");
            {
                var tasks = GetRoleActors("user").Select(
                    actors => actors[0].GracefulStop(TimeSpan.FromMinutes(1),
                                                     new ClientGatewayMessage.Stop()));
                Task.WhenAll(tasks.ToArray()).Wait();
            }

            Console.WriteLine("Shutdown: Users");
            {
                var tasks = GetRoleActors("user-table").Select(
                    actors => actors[0].GracefulStop(TimeSpan.FromMinutes(1),
                                                     new DistributedActorTableMessage<long>.GracefulStop(
                                                         InterfacedPoisonPill.Instance)));
                Task.WhenAll(tasks.ToArray()).Wait();
            }

            Console.WriteLine("Shutdown: Game Pair Maker");
            {
                var tasks = GetRoleActors("game-pair-maker").Select(
                    actors => actors[0].GracefulStop(TimeSpan.FromMinutes(1), InterfacedPoisonPill.Instance));
                Task.WhenAll(tasks.ToArray()).Wait();
            }

            Console.WriteLine("Shutdown: Games");
            {
                var tasks = GetRoleActors("game-table").Select(
                    actors => actors[0].GracefulStop(TimeSpan.FromMinutes(1),
                                                     new DistributedActorTableMessage<long>.GracefulStop(
                                                         InterfacedPoisonPill.Instance)));
                Task.WhenAll(tasks.ToArray()).Wait();
            }

            Console.WriteLine("Shutdown: Systems");
            {
                foreach (var node in Enumerable.Reverse(_nodes))
                    node.System.Terminate();
            }
        }

        private IEnumerable<IActorRef[]> GetRoleActors(string role)
        {
            foreach (var node in _nodes)
            {
                foreach (var ra in node.RoleActors.Where(ra => ra.Role == role))
                {
                    yield return ra.Actors;
                }
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

            return new[] { gateway, container };
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
                    () => new GameBotActor(_clusterContext, new GameRef(gameActor), 0, "bot")));
            }

            return gameActor;
        }
    }
}
