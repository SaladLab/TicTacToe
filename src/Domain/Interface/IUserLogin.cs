using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IUserLogin : IInterfacedActor
    {
        Task<int> Login(string id, string password, int observerId);
    }
}
