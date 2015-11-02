using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IGamePairMaker : IInterfacedActor
    {
        Task RegisterPairing(long userId, string userName, IUserPairingObserver observer);
        Task UnregisterPairing(long userId);
    }
}
