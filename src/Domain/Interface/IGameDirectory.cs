using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IGameDirectory : IInterfacedActor
    {
        Task<IGame> GetOrCreateGame(long id);
        Task RemoveGame(long id);
        Task<List<long>> GetGameList();
        Task RegisterPairing(string userId, IUserPairingObserver observer);
        Task UnregisterPairing(string userId);
    }

    public interface IGameDirectoryWorker : IInterfacedActor
    {
        Task<IGame> CreateGame(long id);
        Task RemoveGame(long id);
    }
}
