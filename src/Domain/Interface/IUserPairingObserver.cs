using System;
using Akka.Interfaced;

namespace Domain.Interface
{
    public interface IUserPairingObserver : IInterfacedObserver
    {
        void MakePair(long gameId, string opponentName);
    }
}
