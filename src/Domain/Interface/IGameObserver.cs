using Akka.Interfaced;
using Domain.Game;

namespace Domain.Interfaced
{
    public interface IGameObserver : IInterfacedObserver
    {
        void Join(int playerId, long userId, string userName);
        void Leave(int playerId);
        void Begin(int currentPlayerId);
        void MakeMove(int playerId, PlacePosition pos, int nextTurnPlayerId);
        void Say(int playerId, string msg);
        void End(int winnerPlayerId);
        void Abort();
    }
}
