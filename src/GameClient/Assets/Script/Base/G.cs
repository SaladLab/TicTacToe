using System.Net;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain.Data;
using Domain.Interfaced;

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
        set
        {
            _comm = value;
            if (_comm != null)
            {
                _comm.ObserverEventPoster = c => ApplicationComponent.Post(c, null);
                _slimRequestWaiter = new SlimRequestWaiter(_comm, ApplicationComponent.Instance);
            }
            else
            {
                _slimRequestWaiter = null;
            }
        }
    }

    private static SlimRequestWaiter _slimRequestWaiter;

    public static SlimRequestWaiter SlimRequestWaiter
    {
        get { return _slimRequestWaiter; }
    }

    public static readonly IPEndPoint ServerEndPoint =
        new IPEndPoint(IPAddress.Loopback, 9001); // new IPEndPoint(IPAddress.Parse("192.168.100.8"), 9001);

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
