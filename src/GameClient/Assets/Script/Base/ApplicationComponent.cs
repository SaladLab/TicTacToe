using System.IO;
using System.Linq;
using System.Net;
using Common.Logging;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Domain.Data;
using Domain.Interfaced;

public class ApplicationComponent : MonoBehaviour, IUserEventObserver
{
    public static ApplicationComponent Instance
    {
        get; private set;
    }

    public static bool TryInit()
    {
        if (Instance != null)
            return false;

        var go = new GameObject("_ApplicationComponent");
        Instance = go.AddComponent<ApplicationComponent>();
        DontDestroyOnLoad(go);
        return true;
    }

    public void Update()
    {
        if (G.Comm != null)
            G.Comm.Update();
    }

    public void UserContextChange(TrackableUserContextTracker userContextTracker)
    {
        G.Logger.InfoFormat("UserContext: {0}", userContextTracker);
        userContextTracker.ApplyTo(G.UserContext);
    }
}
