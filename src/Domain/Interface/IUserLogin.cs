using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    public interface IUserLogin : IInterfacedActor
    {
        Task<Tuple<long, IUserInitiator>> Login(string id, string password);
    }
}
