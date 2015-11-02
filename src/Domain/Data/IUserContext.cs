using System;
using ProtoBuf;
using TrackableData;

namespace Domain.Data
{
    [ProtoContract]
    public interface IUserContext : ITrackableContainer<IUserContext>
    {
        [ProtoMember(1)] TrackableUserData Data { get; set; }
        [ProtoMember(2)] TrackableDictionary<int, UserAchievement> Achivements { get; set; }
    }
}
