using System.IO;
using System.Linq;
using System.Net;
using Common.Logging;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Domain.Interfaced;

public static class G
{
    static G()
    {
        _logger = LogManager.GetLogger("G");
        _unityLogger = new UnityDebugLogger(LogLevel.All);
        _unityLogger.Attach();
    }

    // Communicator

    private static Communicator _comm;

    public static Communicator Comm
    {
        get { return _comm; }
        set
        {
            _comm = value;
        }
    }

    // Logger

    private static ILog _logger;

    public static ILog Logger
    {
        get { return _logger; }
    }

    private static UnityDebugLogger _unityLogger;

    public static UnityDebugLogger UnityLogger
    {
        get { return _unityLogger; }
    }

    // Chat specific data

    public static UserRef User
    {
        get; set;
    }

    public static string UserId
    {
        get; set;
    }
}
