using Domain.Data;
using ProtoBuf;
using TrackableData.Protobuf;

namespace Domain.Workaround
{
    [ProtoContract]
    public class ProtobufSurrogateDirectives
    {
        public TrackableDictionaryTrackerSurrogate<int, UserAchievement> T1;
    }
}
