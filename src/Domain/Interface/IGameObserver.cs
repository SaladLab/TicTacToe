using Akka.Interfaced;
using Domain.Game;

namespace Domain.Interfaced
{
    public interface IGameObserver : IInterfacedObserver
    {
        void Join(int playerId, string userId);
        void Leave(int playerId);
        void Begin(int currentPlayerId);
        void MakeMove(int playerId, PlacePosition pos, int nextTurnPlayerId);
        void Say(int playerId, string msg);
        void End(int winnerPlayerId);
        void Abort();
    }
}
