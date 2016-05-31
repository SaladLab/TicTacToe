using System;
using System.Net;
using Akka.Interfaced.SlimSocket.Base;
using Common.Logging;
using ProtoBuf.Meta;
using TypeAlias;

namespace Akka.Interfaced.SlimSocket.Client
{
    public static class CommunicatorHelper
    {
        public static PacketSerializer CreatePacketSerializer<TTypeModel>()
            where TTypeModel : TypeModel, new()
        {
            return new PacketSerializer(
                new PacketSerializerBase.Data(
                    new ProtoBufMessageSerializer(new TTypeModel()),
                    new TypeAliasTable()));
        }

        public static Communicator CreateCommunicator<TTypeModel>(ILog logger, IPEndPoint remoteEndPoint)
            where TTypeModel : TypeModel, new()
        {
            var serializer = CreatePacketSerializer<TTypeModel>();
            return CreateCommunicator(logger, remoteEndPoint, _ => new TcpConnection(serializer, logger));
        }

        public static Communicator CreateCommunicator(
            ILog logger, IPEndPoint remoteEndPoint, Func<Communicator, TcpConnection> connectionFactory)
        {
            CommunicatorWorker.TryInit();

            var comm = new Communicator(logger, remoteEndPoint, connectionFactory);
            comm.TaskFactory = new SlimTaskFactory { Owner = CommunicatorWorker.Instance };
            comm.ObserverEventPoster = c => CommunicatorWorker.Post(c, null);
            return comm;
        }
    }
}
