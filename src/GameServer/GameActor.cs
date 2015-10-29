using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using Common.Logging;
using Domain.Interfaced;

namespace GameServer
{
    [Log]
    public class GameActor : InterfacedActor<GameActor>, IGame
    {
        Task<GameInfo> IGame.Join(string userId, IGameObserver observer)
        {
            throw new NotImplementedException();
        }

        Task IGame.Leave(string userId)
        {
            throw new NotImplementedException();
        }
    }
}
