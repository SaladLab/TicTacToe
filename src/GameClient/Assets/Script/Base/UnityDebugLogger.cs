using System;
using System.Threading;
using Common.Logging;
using System.Collections.Generic;

public sealed class UnityDebugLogger
{
    private static int _mainThreadId;

    private LogLevel _logLevel;
    private bool _attached;
    private readonly bool _suppressLevel;

    private class LogEntity
    {
        public string Name;
        public LogLevel Level;
        public object Message;
        public Exception Exception;
    }

    private readonly List<LogEntity> _pendingLogs = new List<LogEntity>();

    public UnityDebugLogger(LogLevel logLevel, bool suppressLevel = false)
    {
        if (_mainThreadId == 0)
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

        _logLevel = logLevel;
        _suppressLevel = suppressLevel;
    }

    public LogLevel Level
    {
        get { return _logLevel; }
        set { _logLevel = value; }
    }

    public bool IsInLogging { get; private set; }

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

        if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            Flush();
            WriteLog(source.Name, level, message, exception);
        }
        else
        {
            PendLog(source.Name, level, message, exception);
        }
    }

    private void PendLog(string name, LogLevel level, object message, Exception exception)
    {
        lock (_pendingLogs)
        {
            _pendingLogs.Add(new LogEntity
            {
                Name = name,
                Level = level,
                Message = message,
                Exception = exception
            });
        }
    }

    public void Flush()
    {
        lock (_pendingLogs)
        {
            foreach (var l in _pendingLogs)
            {
                WriteLog(l.Name, l.Level, l.Message, l.Exception);
            }
            _pendingLogs.Clear();
        }
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

            IsInLogging = true;
            UnityEngine.Debug.Log(str);
            IsInLogging = false;
        }
        else
        {
            var str = "[<b>" + name + "</b>] " + message;
            if (exception != null)
                str += "\n" + exception;

            IsInLogging = true;
            if (level < LogLevel.Warn)
                UnityEngine.Debug.Log(str);
            else if (level == LogLevel.Warn)
                UnityEngine.Debug.LogWarning(str);
            else if (level >= LogLevel.Error)
                UnityEngine.Debug.LogError(str);
            IsInLogging = false;
        }
    }
}
