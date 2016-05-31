using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    public interface IUser : IInterfacedActor
    {
        Task RegisterPairing(IUserPairingObserver observer);
        Task UnregisterPairing();
        Task<Tuple<IGamePlayer, int, GameInfo>> JoinGame(long gameId, IGameObserver observer);
        Task LeaveGame(long gameId);
    }
}
