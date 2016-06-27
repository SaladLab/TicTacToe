using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Utility;
using Akka.Configuration;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket;

namespace GameServer
{
    public class ClusterRunner
    {
        private readonly Config _commonConfig;

        public class Node
        {
            public ClusterNodeContext Context;
            public ClusterRoleWorker[] Workers;
        }

        private readonly List<Node> _nodes = new List<Node>();

        public ClusterRunner(Config commonConfig)
        {
            _commonConfig = commonConfig;
        }

        public async Task LaunchNode(int port, int clientPort, params string[] roles)
        {
            // setup system

            var config = _commonConfig
                .WithFallback("akka.remote.helios.tcp.port = " + port)
                .WithFallback("akka.cluster.roles = " + "[" + string.Join(",", roles) + "]");

            var system = ActorSystem.Create("GameCluster", config);
            DeadRequestProcessingActor.Install(system);

            // configure cluster base utilities

            var cluster = Cluster.Get(system);
            var context = new ClusterNodeContext { System = system };

            context.ClusterActorDiscovery = system.ActorOf(Props.Create(() => new ClusterActorDiscovery(cluster)), "ClusterActorDiscovery");
            context.ClusterNodeContextUpdater = system.ActorOf(Props.Create(() => new ClusterNodeContextUpdater(context)), "ClusterNodeContextUpdater");

            // start workers by roles

            var workers = new List<ClusterRoleWorker>();
            foreach (var role in roles)
            {
                ClusterRoleWorker worker;
                switch (role)
                {
                    case "user-table":
                        worker = new UserTableWorker(context);
                        break;

                    case "user":
                        worker = new UserWorker(context, ChannelType.Tcp, new IPEndPoint(IPAddress.Any, clientPort));
                        break;

                    case "game-pair-maker":
                        worker = new GamePairMakerWorker(context);
                        break;

                    case "game-table":
                        worker = new GameTableWorker(context);
                        break;

                    case "game":
                        worker = new GameWorker(context);
                        break;

                    default:
                        throw new InvalidOperationException("Invalid role: " + role);
                }
                await worker.Start();
                workers.Add(worker);
            }

            _nodes.Add(new Node { Context = context, Workers = workers.ToArray() });
        }

        public async Task Shutdown()
        {
            // stop all workers

            foreach (var node in _nodes.AsEnumerable().Reverse())
            {
                foreach (var worker in node.Workers.Reverse())
                {
                    await worker.Stop();
                    Console.WriteLine($"Shutdown: Worker({worker.GetType().Name})");
                }
            }

            // stop all actor systems

            Console.WriteLine("Shutdown: Systems");
            await Task.WhenAll(_nodes.AsEnumerable().Reverse().Select(n => n.Context.System.Terminate()));
        }
    }
}
