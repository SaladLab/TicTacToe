using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;
using System.Net;
using Domain.Interfaced;

namespace GameServer
{
    internal static class LinkDirectives
    {
        public static void Link()
        {
            // http://stackoverflow.com/questions/23545272/dll-reference-not-copying-into-project-bin
            var objs = new object[]
            {
                typeof(Akka.Interfaced.ProtobufSerializer.ProtobufSerializer)
            };
            if (objs.Any(o => o == null))
                throw new InvalidOperationException();
        }
    }
}
