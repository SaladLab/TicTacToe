using Common.Logging;
using Domain.Interfaced;

public static class G
{
    static G()
    {
        _logger = LogManager.GetLogger("G");
        _debugLogAdapter = new UnityDebugLogAdapter(LogLevel.All);
        _debugLogAdapter.Attach();
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

    private static readonly ILog _logger;

    public static ILog Logger
    {
        get { return _logger; }
    }

    private static readonly UnityDebugLogAdapter _debugLogAdapter;

    public static UnityDebugLogAdapter DebugLogAdapter
    {
        get { return _debugLogAdapter; }
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
