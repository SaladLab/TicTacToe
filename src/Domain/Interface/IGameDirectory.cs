using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Interfaced;
using ProtoBuf;

namespace Domain.Interfaced
{
    [ProtoContract]
    public class CreateGameParam
    {
        [ProtoMember(1)] public bool WithBot;
    };

    public interface IGameDirectory : IInterfacedActor
    {
        Task<IGame> GetGame(long id);
        Task<Tuple<long, IGame>> CreateGame(CreateGameParam param);
        Task RemoveGame(long id);
        Task<List<long>> GetGameList();
    }

    public interface IGameDirectoryWorker : IInterfacedActor
    {
        Task<IGame> CreateGame(long id, CreateGameParam param);
        Task RemoveGame(long id);
    }
}
