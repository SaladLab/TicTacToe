using System;
using ProtoBuf;
using TrackableData;

namespace Domain
{
    [ProtoContract]
    public interface IUserData : ITrackablePoco<IUserData>
    {
        [ProtoMember(1)] string Name { get; set; }
        [ProtoMember(2)] DateTime RegisterTime { get; set; }
        [ProtoMember(3)] DateTime LastLoginTime { get; set; }
        [ProtoMember(4)] int LoginCount { get; set; }
        [ProtoMember(5)] int PlayCount { get; set; }
        [ProtoMember(6)] int WinCount { get; set; }
        [ProtoMember(7)] int LoseCount { get; set; }
        [ProtoMember(8)] int DrawCount { get; set; }
    }
}
