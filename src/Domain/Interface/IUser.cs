using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IUser : IInterfacedActor
    {
        Task RegisterPairing(int observerId);
        Task UnregisterPairing();
        Task<Tuple<int, GameInfo>> JoinGame(long gameId, int observerId);
        Task LeaveGame(long gameId);
    }
}
