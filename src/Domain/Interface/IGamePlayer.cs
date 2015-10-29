using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Akka.Interfaced;

namespace Domain.Interfaced
{
    // Player who is playing game
    [TagOverridable("playerUserId")]
    public interface IGamePlayer : IInterfacedActor
    {
        Task MakeMove(PlacePosition pos, string playerUserId = null);
        Task Say(string msg, string playerUserId = null);
    }
}
