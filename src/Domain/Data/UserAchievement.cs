using System;
using ProtoBuf;
using TrackableData;

namespace Domain.Data
{
    public enum AchievementKey
    {
        FirstPlay = 1,
        FirstWin = 2,
        FirstLose = 3,
        FirstDraw = 4,
        Play10Times = 11,
        Win10Times = 12,
        Lose10Times = 13,
        Draw10Times = 14,
    }

    [ProtoContract]
    public class UserAchievement
    {
        [ProtoMember(1)] public DateTime? AchieveTime { get; set; }
        [ProtoMember(2)] public int Value { get; set; }

        public override string ToString()
        {
            if (AchieveTime.HasValue && Value != 0)
                return $"Achieved({AchieveTime}), Value({Value})";
            if (AchieveTime.HasValue)
                return $"Achieved({AchieveTime})";
            if (Value != 0)
                return $"Value({Value})";
            return string.Empty;
        }
    }

    public static class UserAchievementDictionaryExtensions
    {
        public static bool IsAchieved(this TrackableDictionary<int, UserAchievement> dict,
                                      AchievementKey key)
        {
            UserAchievement ach;
            return dict.TryGetValue((int)key, out ach) && ach.AchieveTime.HasValue;
        }

        public static bool TryAchieved(this TrackableDictionary<int, UserAchievement> dict,
                                       AchievementKey key)
        {
            UserAchievement ach;
            if (dict.TryGetValue((int)key, out ach))
            {
                if (ach.AchieveTime.HasValue)
                    return false;

                ach = new UserAchievement { AchieveTime = DateTime.UtcNow, Value = ach.Value };
                dict[(int)key] = ach;
            }
            else
            {
                ach = new UserAchievement { AchieveTime = DateTime.UtcNow };
                dict.Add((int)key, ach);
            }
            return true;
        }

        public static int? TryProgress(this TrackableDictionary<int, UserAchievement> dict,
                                       AchievementKey key, int increment)
        {
            UserAchievement ach;
            if (dict.TryGetValue((int)key, out ach))
            {
                if (ach.AchieveTime.HasValue)
                    return null;

                ach = new UserAchievement { Value = ach.Value + increment };
                dict[(int)key] = ach;
            }
            else
            {
                ach = new UserAchievement { Value = increment };
                dict.Add((int)key, ach);
            }
            return ach.Value;
        }

        public static bool TryProgress(this TrackableDictionary<int, UserAchievement> dict,
                                       AchievementKey key, int increment, int goal)
        {
            var value = TryProgress(dict, key, increment);
            if (value == null || value < goal)
                return false;

            return TryAchieved(dict, key);
        }
    }
}
