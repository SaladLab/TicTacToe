using System;
using Akka.Interfaced;
using Domain;

namespace Domain
{
    public interface IUserEventObserver : IInterfacedObserver
    {
        void UserContextChange(TrackableUserContextTracker userContextTracker);
    }
}
