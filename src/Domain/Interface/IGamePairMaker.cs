using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IGamePairMaker : IInterfacedActor
    {
        Task RegisterPairing(string userId, IUserPairingObserver observer);
        Task UnregisterPairing(string userId);
    }
}
