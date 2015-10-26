using System;
using System.Net;
using System.Net.Sockets;
using Common.Logging;
using Akka.Actor;
using Akka.Interfaced.SlimSocketServer;
using Akka.Interfaced;
using System.Threading.Tasks;
using System.Collections.Generic;
using Domain.Interfaced;

namespace GameServer
{
    public class ClientGatewayMessage
    {
        public class Start
        {
            public IPEndPoint ServiceEndPoint;
        }

        public class Accept
        {
            public Socket Socket;
        }
    }

    public class ClientGateway : ReceiveActor
    {
        private ILog _logger = LogManager.GetLogger("ClientGateway");
        private ClusterNodeContext _context;
        private TcpAcceptor _tcpAcceptor;
        private HashSet<IActorRef> _sessionSet = new HashSet<IActorRef>();
        private bool _isStopped;

        public ClientGateway(ClusterNodeContext context)
        {
            _context = context;

            _context.ClusterNodeActor.Tell(
                new ActorDiscoveryMessage.WatchActor { Type = typeof(IUserDirectory) },
                Self);

            Receive<ClientGatewayMessage.Start>(m => Handle(m));
            Receive<ClientGatewayMessage.Accept>(m => Handle(m));
            Receive<ShutdownMessage>(m => Handle(m));
            Receive<Terminated>(m => Handle(m));
        }

        private void Handle(ClientGatewayMessage.Start m)
        {
            _logger.Info("Start");

            try
            {
                var self = Self;
                _tcpAcceptor = new TcpAcceptor();
                _tcpAcceptor.Accepted += (sender, socket) =>
                {
                    self.Tell(new ClientGatewayMessage.Accept { Socket = socket }, self);
                    return TcpAcceptor.AcceptResult.Accept;
                };
                _tcpAcceptor.Listen(m.ServiceEndPoint);
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Start got exception.", e);
                return;
            }
        }

        private void Handle(ClientGatewayMessage.Accept m)
        {
            if (_isStopped)
                return;

            _logger.InfoFormat("Accept {0}", m.Socket.RemoteEndPoint);

            var clientSession = Context.ActorOf(
                Props.Create<ClientSession>(_context, m.Socket));
            Context.Watch(clientSession);
            _sessionSet.Add(clientSession);
        }

        private void Handle(ShutdownMessage m)
        {
            if (_isStopped)
                return;

            _logger.Info("Stop");
            _isStopped = true;
            
            // stop listening

            if (_tcpAcceptor != null)
            {
                _tcpAcceptor.Close();
                _tcpAcceptor = null;
            }

            // stop all running client sessions

            if (_sessionSet.Count > 0)
            {
                Context.ActorSelection("*").Tell(PoisonPill.Instance);
            }
            else
            {
                Context.Stop(Self);
            }
        }

        private void Handle(Terminated m)
        {
            _sessionSet.Remove(m.ActorRef);

            if (_isStopped && _sessionSet.Count == 0)
                Context.Stop(Self);
        }
    }
}
