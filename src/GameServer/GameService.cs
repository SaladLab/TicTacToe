using System;
using System.Configuration;
using Aim.ClusterNode;
using Akka.Configuration.Hocon;
using Common.Logging;
using Domain;
using Topshelf;

namespace GameServer
{
    public class GameService : ServiceControl
    {
        private ILog _log = LogManager.GetLogger("GameService");
        private ClusterRunner _clusterRunner;
        private string _runner;

        public GameService(string runner)
        {
            _runner = runner;
        }

        bool ServiceControl.Start(HostControl hostControl)
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
                _log.Error("MongoDB connection error", e);
                return false;
            }

            // run cluster nodes

            var section = (AkkaConfigurationSection)ConfigurationManager.GetSection("akka");
            var config = section.AkkaConfig;
            var runner = new ClusterRunner(config, new[] { GetType().Assembly });
            runner.CreateClusterNodeContext = () => new ClusterNodeContext();

            var runnerConfig = config.GetValue("system.runner").GetObject();
            var nodes = runnerConfig.GetKey(_runner ?? "default");
            if (nodes == null)
            {
                _log.Error("Cannot find runner:" + _runner);
                return false;
            }

            runner.Launch(nodes.GetArray()).Wait();
            _clusterRunner = runner;

            return true;
        }

        bool ServiceControl.Stop(HostControl hostControl)
        {
            _log.Info("Stop");

            if (_clusterRunner != null)
            {
                _clusterRunner.Shutdown().Wait();
                _clusterRunner = null;
            }
            return true;
        }
    }
}
