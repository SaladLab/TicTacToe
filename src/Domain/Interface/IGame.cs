using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public class CreateGameParam
    {
        public bool WithBot;
    }

    public interface IGame : IInterfacedActor
    {
        Task<Tuple<int, GameInfo>> Join(long userId, string userName,
                                        IGameObserver observer, IGameUserObserver observerForUserActor);
        Task Leave(long userId);
    }
}
