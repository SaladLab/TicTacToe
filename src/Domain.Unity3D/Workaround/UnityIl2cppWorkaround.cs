using System;
using System.Linq;
using Domain.Data;
using TrackableData;

namespace Domain.Workaround
{
    public class UnityIl2cppWorkaround
    {
        public static void Initialize()
        {
            if (DateTime.UtcNow.Year > 1)
                return;

            if (StubForTracker().Count(o => o == null) > 0)
                throw new Exception("Il2cppWorkaround got an error!");
        }

        /*
            Without this in IL2CPP, we can get this exception.

            MissingMethodException: Method not found: 
                'Default constructor not found...ctor() of TrackableData.TrackableDictionaryTracker`2[
                [System.Int32, mscorlib, Version=2.0.5.0, Culture=, PublicKeyToken=7cec85d7bea7798e],
                [System.String, mscorlib, Version=2.0.5.0, Culture=, PublicKeyToken=7cec85d7bea7798e]]'.
        */

        private static object[] StubForTracker()
        {
            return new object[]
            {
                new TrackablePocoTracker<IUserData>(),
                new TrackableDictionaryTracker<int, UserAchievement>(),
            };
        }
    }
}
