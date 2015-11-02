using Domain.Data;
using ProtoBuf;
using TrackableData.Protobuf;

namespace Domain.Workaround
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class ProtobufPrecompileDirectives
    {
        public TrackableDictionaryTrackerSurrogate<int, UserAchievement> T1;
    }
}
