using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IGameDirectory : IInterfacedActor
    {
        Task RegisterPairing(string userId);
        Task UnregisterPairing();
        Task<IGame> GetOrCreateGame(long id);
        Task RemoveGame(long id);
        Task<List<long>> GetGameList();
    }

    public interface IGameDirectoryWorker : IInterfacedActor
    {
        Task<IGame> CreateGame(long id);
        Task RemoveGame(long id);
    }
}
