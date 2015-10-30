using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IGame : IInterfacedActor
    {
        Task<GameInfo> Join(string userId, IGameObserver observer);
        Task Leave(string userId);
    }
}
