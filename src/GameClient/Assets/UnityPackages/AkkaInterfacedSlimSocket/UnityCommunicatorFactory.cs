using System;
using System.Net;
using Akka.Interfaced.SlimSocket;
using Common.Logging;
using ProtoBuf.Meta;

namespace Akka.Interfaced.SlimSocket.Client
{
    public static class UnityCommunicatorFactory
    {
        public static Communicator Create()
        {
            ChannelEventDispatcher.TryInit();

            var communicator = new Communicator();
            communicator.ChannelFactory.TaskFactory = new UnitySlimTaskFactory { Owner = ChannelEventDispatcher.Instance };
            ((ObserverRegistry)communicator.ObserverRegistry).EventPoster = c => ChannelEventDispatcher.Post(c, null);

            return communicator;
        }
    }
}
