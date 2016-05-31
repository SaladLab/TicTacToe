using Domain;
using ProtoBuf;
using TrackableData.Protobuf;

namespace Domain
{
    [ProtoContract]
    public class ProtobufSurrogateDirectives
    {
        public TrackableDictionaryTrackerSurrogate<int, UserAchievement> T1;
    }
}
