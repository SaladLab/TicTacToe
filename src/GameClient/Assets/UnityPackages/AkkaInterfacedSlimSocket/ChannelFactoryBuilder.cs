using System;
using System.Net;
using Akka.Interfaced.SlimSocket;
using Common.Logging;
using ProtoBuf.Meta;
using TypeAlias;

namespace Akka.Interfaced.SlimSocket.Client
{
    public static class ChannelFactoryBuilder
    {
        public static ChannelFactory Build<TTypeModel>(IPEndPoint endPoint = null, Func<ILog> createChannelLogger = null)
            where TTypeModel : TypeModel, new()
        {
            var serializer = PacketSerializer.CreatePacketSerializer<TTypeModel>();
            return Build(serializer, endPoint, createChannelLogger);
        }

        public static ChannelFactory Build(IPacketSerializer packetSerializer = null, IPEndPoint endPoint = null, Func<ILog> createChannelLogger = null)
        {
            ChannelEventDispatcher.TryInit();

            if (packetSerializer == null)
                packetSerializer = PacketSerializer.CreatePacketSerializer();

            var channelFactory = new ChannelFactory
            {
                Type = ChannelType.Tcp,
                ConnectEndPoint = endPoint,
                CreateChannelLogger = createChannelLogger,
                TaskFactory = new UnitySlimTaskFactory { Owner = ChannelEventDispatcher.Instance },
                ObserverEventPoster = c => ChannelEventDispatcher.Post(c, null),
                PacketSerializer = packetSerializer
            };

            return channelFactory;
        }
    }
}
