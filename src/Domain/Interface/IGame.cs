using System;
using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;
using TypeAlias;
using System.Collections.Generic;

namespace Domain.Interfaced
{
    public interface IGame : IInterfacedActor
    {
        Task<GameInfo> Join(string userId, IGameObserver observer);
        Task Leave(string userId);
    }
}
