using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;
using TypeAlias;
using System.Collections.Generic;

namespace Domain.Interfaced
{
    public interface IGame : IInterfacedActor
    {
    }

    [ProtoContract]
    public class GameInfo
    {
        [ProtoMember(1)] public string Name;
    }
}
