using System;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IUserPairingObserver : IInterfacedObserver
    {
        void MakePair(long gameId, string opponentName);
    }
}
