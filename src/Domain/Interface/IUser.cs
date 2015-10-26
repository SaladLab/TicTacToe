using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IUser : IInterfacedActor
    {
        Task<string> GetId();
    }
}
