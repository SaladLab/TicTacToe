using System;
using System.Threading;
using Common.Logging;
using System.Collections.Generic;

public sealed class UnityDebugLogAdapter
{
    private LogLevel _logLevel;
    private bool _attached;
    private readonly bool _suppressLevel;

    /// <summary>
    /// Construct 
    /// </summary>
    /// <param name="logLevel">
    ///     Logger will write log if level of log greater than or equal to logLevel.
    /// </param>
    /// <param name="suppressLevel">
    ///     If suppressLevel on, log will be written with Debug.Log regardless of level of log.
    ///     But you can notice level of log by color and prefix of message
    /// </param>
    public UnityDebugLogAdapter(LogLevel logLevel, bool suppressLevel = false)
    {
        _logLevel = logLevel;
        _suppressLevel = suppressLevel;
    }

    public LogLevel Level
    {
        get { return _logLevel; }
        set { _logLevel = value; }
    }

    public void Attach()
    {
        if (_attached)
            return;

        LogManager.Log += OnLog;
        _attached = true;
    }

    public void Detach()
    {
        if (!_attached)
            return;

        LogManager.Log -= OnLog;
        _attached = false;
    }

    private void OnLog(LogSource source, LogLevel level, object message, Exception exception)
    {
        if (level < _logLevel)
            return;

        WriteLog(source.Name, level, message, exception);
    }

    private static readonly string[] LevelStrings =
    {
        "",
        "<color=grey>(T)</color>",
        "<color=grey>(D)</color>",
        "<color=white>(I)</color>",
        "<color=orange>(W)</color>",
        "<color=red>(E)</color>",
        "<color=red>(F)</color>",
        ""
    };

    private void WriteLog(string name, LogLevel level, object message, Exception exception)
    {
        if (_suppressLevel)
        {
            var str = "[<b>" + name + "</b>] " + LevelStrings[(int)level] + " " + message;
            if (exception != null)
                str += "\n" + exception;

            UnityEngine.Debug.Log(str);
        }
        else
        {
            var str = "[<b>" + name + "</b>] " + message;
            if (exception != null)
                str += "\n" + exception;

            if (level < LogLevel.Warn)
                UnityEngine.Debug.Log(str);
            else if (level == LogLevel.Warn)
                UnityEngine.Debug.LogWarning(str);
            else if (level >= LogLevel.Error)
                UnityEngine.Debug.LogError(str);
        }
    }
}
