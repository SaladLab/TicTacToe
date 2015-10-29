using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;
using System.Collections.Generic;

namespace Domain.Interfaced
{
    [ProtoContract]
    public class GameInfo
    {
        [ProtoMember(1)] public long Id;
        [ProtoMember(2)] public string[] PlayerNames;
        [ProtoMember(3)] public int FirstMovePlayerId;
        [ProtoMember(4)] public List<PlacePosition> Positions;
    }

    [ProtoContract]
    public class PlacePosition
    {
        [ProtoMember(1)] public int X;
        [ProtoMember(2)] public int Y;
    }
}
