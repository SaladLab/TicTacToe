using System;
using Common.Logging;
using Common.Logging.Factory;

public class LogSource : AbstractLogger
{
    private string _name;

    public string Name
    {
        get { return _name; }
    }

    public LogSource(string name)
    {
        _name = name;
    }

    protected override void WriteInternal(LogLevel level, object message, Exception exception)
    {
        LogManager.OnLog(this, level, message, exception);
    }

    public override bool IsTraceEnabled
    {
        get { return true; }
    }

    public override bool IsDebugEnabled
    {
        get { return true; }
    }

    public override bool IsErrorEnabled
    {
        get { return true; }
    }

    public override bool IsFatalEnabled
    {
        get { return true; }
    }

    public override bool IsInfoEnabled
    {
        get { return true; }
    }

    public override bool IsWarnEnabled
    {
        get { return true; }
    }
}
