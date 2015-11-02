using System;
using ProtoBuf;

namespace Domain.Data
{
    public enum AchievementKey
    {
        FirstWin = 1,
        FirstLose = 2,
        FirstDraw = 3,
        BeatUser5Times = 11,
        BeatBot5Times = 12,
    }

    [ProtoContract]
    public class UserAchievement
    {
        [ProtoMember(1)] public int Value { get; set; }
        [ProtoMember(2)] public DateTime CreateTime { get; set; }
        [ProtoMember(3)] public DateTime? AchieveTime { get; set; }
    }
}
