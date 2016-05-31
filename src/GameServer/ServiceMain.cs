using System;
using System.Configuration;
using Akka.Configuration;
using Domain;

namespace GameServer
{
    public class ServiceMain
    {
        public void Run(string[] args)
        {
            // force interface assembly to be loaded before creating ProtobufSerializer

            var type = typeof(IUser);
            if (type == null)
                throw new InvalidProgramException("!");

            // connect to mongo-db

            try
            {
                var cstr = ConfigurationManager.ConnectionStrings["MongoDb"].ConnectionString;
                MongoDbStorage.Instance = new MongoDbStorage(cstr);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in connecting mongo-db: " + e);
                return;
            }

            // run cluster nodes

            var commonConfig = ConfigurationFactory.ParseString(@"
                akka {
                  actor {
                    provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                    serializers {
                      wire = ""Akka.Serialization.WireSerializer, Akka.Serialization.Wire""
                      proto = ""Akka.Interfaced.ProtobufSerializer.ProtobufSerializer, Akka.Interfaced.ProtobufSerializer""
                    }
                    serialization-bindings {
                      ""Akka.Interfaced.NotificationMessage, Akka.Interfaced-Base"" = proto
                      ""Akka.Interfaced.RequestMessage, Akka.Interfaced-Base"" = proto
                      ""Akka.Interfaced.ResponseMessage, Akka.Interfaced-Base"" = proto
                      ""System.Object"" = wire
                    }
                  }
                  remote {
                    helios.tcp {
                      hostname = ""127.0.0.1""
                    }
                  }
                  cluster {
                    seed-nodes = [""akka.tcp://GameCluster@127.0.0.1:3001""]
                    auto-down-unreachable-after = 30s
                  }
                }");

            var clusterRunner = new ClusterRunner(commonConfig);

            var standAlone = args.Length > 0 && args[0] == "standalone";
            if (standAlone)
            {
                clusterRunner.LaunchNode(3001, 9001, "game-pair-maker", "game-table", "game", "user-table", "user");
            }
            else
            {
                clusterRunner.LaunchNode(3001, 0, "game-table", "game-pair-maker");
                clusterRunner.LaunchNode(3002, 0, "user-table");
                clusterRunner.LaunchNode(3011, 0, "game");
                clusterRunner.LaunchNode(3012, 0, "game");
                clusterRunner.LaunchNode(3021, 9001, "user");
                clusterRunner.LaunchNode(3022, 9002, "user");
            }

            // wait for stop signal

            Console.WriteLine("Press enter key to exit.");
            Console.ReadLine();

            clusterRunner.Shutdown();
        }
    }
}
