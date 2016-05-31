using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Interfaced;
using Domain;

namespace Domain
{
    // Player who is playing game
    [TagOverridable("playerUserId")]
    public interface IGamePlayer : IInterfacedActor
    {
        Task MakeMove(PlacePosition pos, long playerUserId = 0);
        Task Say(string msg, long playerUserId = 0);
    }
}
