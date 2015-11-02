using System.Net;
using Common.Logging;
using Domain.Data;
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

    public static readonly IPEndPoint ServerEndPoint =
        new IPEndPoint(IPAddress.Loopback, 9001); // new IPEndPoint(IPAddress.Parse("192.168.100.8"), 9001);

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

    public static long UserId
    {
        get; set;
    }

    public static TrackableUserContext UserContext
    {
        get; set;
    }
}
