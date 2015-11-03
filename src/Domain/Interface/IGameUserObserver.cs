using Akka.Interfaced;
using Domain.Game;

namespace Domain.Interfaced
{
    // It's an observer monitoring major events for IUserActor
    public interface IGameUserObserver : IInterfacedObserver
    {
        void Begin(long gameId);
        void End(long gameId, GameResult result);
    }
}
