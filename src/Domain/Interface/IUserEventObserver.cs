using System;
using Akka.Interfaced;
using Domain.Data;

namespace Domain.Interface
{
    public interface IUserEventObserver : IInterfacedObserver
    {
        void UserContextChange(TrackableUserContextTracker userContextTracker);
    }
}
