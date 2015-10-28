using System;
using Common.Logging;

public static class LogManager
{
    public delegate void LogHandler(LogSource source, LogLevel level, object message, Exception exception);

    public static event LogHandler Log;

    public static ILog GetLogger(string name)
    {
        return new LogSource(name);
    }

    internal static void OnLog(LogSource source, LogLevel level, object message, Exception exception)
    {
        if (Log != null)
            Log(source, level, message, exception);
    }
}
