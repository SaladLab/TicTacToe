using System;
using Akka.Interfaced;
using Domain.Data;

namespace Domain.Interfaced
{
    public interface IUserEventObserver : IInterfacedObserver
    {
        void UserContextChange(TrackableUserContextTracker userContextTracker);
    }
}
