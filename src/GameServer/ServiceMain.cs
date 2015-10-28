using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Interfaced;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    public class ServiceMain
    {
        private List<Tuple<ActorSystem, List<IActorRef>>> _clusters = new List<Tuple<ActorSystem, List<IActorRef>>>();

        public void Run(string[] args)
        {
            // force interface assembly to be loaded before creating ProtobufSerializer

            var type = typeof(IUser);
            if (type == null)
                throw new InvalidProgramException("!");

            // connect to redis

            try
            {
                var cstr = ConfigurationManager.ConnectionStrings["MongoDb"].ConnectionString;
                MongoDbStorage.Instance = new MongoDbStorage(cstr);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in connecting redis server: " + e);
                return;
            }

            // run cluster nodes

            var commonConfig = ConfigurationFactory.ParseString(@"
                akka {
                  actor {
                    provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                    serializers {
                      proto = ""Akka.Interfaced.ProtobufSerializer.ProtobufSerializer, Akka.Interfaced-ProtobufSerializer""
                    }
                    serialization-bindings {
                      ""Akka.Interfaced.NotificationMessage, Akka.Interfaced"" = proto
                      ""Akka.Interfaced.RequestMessage, Akka.Interfaced"" = proto
                      ""Akka.Interfaced.ResponseMessage, Akka.Interfaced"" = proto
                    }
                  }
                  remote {
                    helios.tcp {
                      hostname = ""127.0.0.1""
                    }
                  }
                  cluster {
                    seed-nodes = [""akka.tcp://GameCluster@127.0.0.1:3002""]
                    auto-down-unreachable-after = 30s
                  }
                }");

            var standAlone = args.Length > 0 && args[0] == "standalone";
            if (standAlone)
            {
                LaunchClusterNode(commonConfig, 3001, 9001, "room-directory", "user-directory", "room", "user", "bot");
            }
            else
            {
                //LaunchClusterNode(commonConfig, 3001, 0, "room-directory");
                LaunchClusterNode(commonConfig, 3002, 0, "user-directory");
                //LaunchClusterNode(commonConfig, 3011, 0, "room");
                //LaunchClusterNode(commonConfig, 3012, 0, "room");
                LaunchClusterNode(commonConfig, 3021, 9001, "user");
                LaunchClusterNode(commonConfig, 3022, 9002, "user");
                //LaunchClusterNode(commonConfig, 3031, 0, "bot");
            }

            // wait for stop signal

            Console.WriteLine("Press enter key to exit.");
            Console.ReadLine();

            // TODO: Graceful Shutdown

            _clusters.Reverse();
            foreach (var cluster in _clusters)
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

        private void LaunchClusterNode(Config commonConfig, int port, int clientPort, params string[] roles)
        {
            var config = commonConfig
                .WithFallback("akka.remote.helios.tcp.port = " + port)
                .WithFallback("akka.cluster.roles = " + "[" + string.Join(",", roles) + "]");
            var system = ActorSystem.Create("GameCluster", config);
            var rootActors = InitClusterNode(system, clientPort, roles);
            _clusters.Add(Tuple.Create(system, rootActors));
        }

        private static List<IActorRef> InitClusterNode(ActorSystem system, int clientPort, params string[] roles)
        {
            var context = new ClusterNodeContext { System = system };
            DeadRequestProcessingActor.Install(system);
            context.ClusterNodeActor = system.ActorOf(
                Props.Create<ClusterNodeActor>(context),
                "cluster");

            var rootActors = new List<IActorRef>();
            foreach (var role in roles)
            {
                IActorRef rootActor = null;
                switch (role)
                {
                    //case "room-directory":
                    //    rootActor = system.ActorOf(Props.Create<RoomDirectoryActor>(context), "room_directory");
                    //    break;

                    case "user-directory":
                        rootActor = system.ActorOf(Props.Create<UserDirectoryActor>(context), "user_directory");
                        break;

                    //case "room":
                    //    rootActor = system.ActorOf(Props.Create<RoomDirectoryWorkerActor>(context), "room_directory_worker");
                    //    break;

                    case "user":
                        rootActor = system.ActorOf(Props.Create<ClientGateway>(context), "client_gateway");
                        rootActor.Tell(new ClientGatewayMessage.Start { ServiceEndPoint = new IPEndPoint(IPAddress.Any, clientPort) });
                        break;

                    //case "bot":
                    //    rootActor = system.ActorOf(Props.Create<ChatBotCommanderActor>(context), "chatbot_commander");
                    //    rootActor.Tell(new ChatBotCommanderMessage.Start());
                    //    break;

                    default:
                        throw new InvalidOperationException("Invalid role: " + role);
                }
                rootActors.Add(rootActor);
            }
            return rootActors;
        }
    }
}
