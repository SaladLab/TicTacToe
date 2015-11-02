using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IUserDirectory : IInterfacedActor
    {
        Task RegisterUser(long userId, IUser user);
        Task UnregisterUser(long userId);
        Task<IUser> GetUser(long userId);
    }
}
