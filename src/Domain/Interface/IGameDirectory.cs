using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Interfaced;
using ProtoBuf;
using TypeAlias;

namespace Domain.Interfaced
{
    public interface IGameDirectory : IInterfacedActor
    {
        Task<IGame> GetOrCreateGame(string name);
        Task RemoveGame(string name);
        Task<List<string>> GetGameList();
    }

    public interface IGameDirectoryWorker : IInterfacedActor
    {
        Task<IGame> CreateGame(string name);
        Task RemoveGame(string name);
    }
}
