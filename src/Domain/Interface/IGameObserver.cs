using System;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    public interface IGameObserver : IInterfacedObserver
    {
        void Join(int playerId, string userId);
        void Leave(int playerId);
        void MakeMove(int playerId, PlacePosition pos);
        void Say(int playerId, string msg);
    }
}
