using System.Collections.Generic;
using ProtoBuf;

namespace Domain
{
    public enum GameState
    {
        WaitingForPlayers,
        Playing,
        Ended,
        Aborted,
    }

    public enum GameResult
    {
        None,
        Win,
        Lose,
        Draw,
    }

    [ProtoContract]
    public class GameInfo
    {
        [ProtoMember(1)] public long Id;
        [ProtoMember(2)] public GameState State;
        [ProtoMember(3)] public List<string> PlayerNames;
        [ProtoMember(4)] public int FirstMovePlayerId;
        [ProtoMember(5)] public List<PlacePosition> Positions;
    }
}
