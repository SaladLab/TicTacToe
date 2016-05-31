using Domain;

public class UserEventProcessor : IUserEventObserver
{
    private static UserEventProcessor s_instance;

    public static UserEventProcessor Instance
    {
        get
        {
            if (s_instance == null)
                s_instance = new UserEventProcessor();
            return s_instance;
        }
    }

    void IUserEventObserver.UserContextChange(TrackableUserContextTracker userContextTracker)
    {
        G.Logger.InfoFormat("UserContext: {0}", userContextTracker);
        userContextTracker.ApplyTo(G.UserContext);
    }
}
