using System.Threading.Tasks;
using Akka.Interfaced;
using Domain.Data;
using ProtoBuf;

namespace Domain.Interface
{
    [ProtoContract]
    public class LoginResult
    {
        [ProtoMember(1)] public long UserId;
        [ProtoMember(2)] public IUser User;
        [ProtoMember(3)] public TrackableUserContext UserContext;
    }

    public interface IUserLogin : IInterfacedActor
    {
        Task<LoginResult> Login(string id, string password, IUserEventObserver observer);
    }
}
