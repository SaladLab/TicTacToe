using System.Net;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain.Data;
using Domain.Interface;

public static class G
{
    static G()
    {
        _logger = LogManager.GetLogger("G");
    }

    // Communicator

    private static Communicator _comm;

    public static Communicator Comm
    {
        get { return _comm; }
        set { _comm = value; }
    }

    public static readonly IPEndPoint DefaultServerEndPoint = new IPEndPoint(IPAddress.Loopback, 9001);

    // Logger

    private static readonly ILog _logger;

    public static ILog Logger
    {
        get { return _logger; }
    }

    // User specific data

    public static UserRef User { get; set; }

    public static long UserId { get; set; }

    public static TrackableUserContext UserContext { get; set; }
}
