using System;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IUserEventObserver : IInterfacedObserver
    {
        void MakePair(long gameId, string opponentName);
    }
}
