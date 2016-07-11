using System;
using System.Threading.Tasks;
using Akka.Interfaced;

namespace Domain
{
    public interface IUserInitiator : IInterfacedActor
    {
        Task<TrackableUserContext> Create(IUserEventObserver observer, string name);
        Task<TrackableUserContext> Load(IUserEventObserver observer);
    }
}
