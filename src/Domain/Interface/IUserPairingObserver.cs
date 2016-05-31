using Akka.Interfaced;

namespace Domain
{
    public interface IUserPairingObserver : IInterfacedObserver
    {
        void MakePair(long gameId, string opponentName);
    }
}
