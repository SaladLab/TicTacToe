using System;
using System.Linq;
using System.Net;

// from http://stackoverflow.com/questions/2727609/best-way-to-create-ipendpoint-from-string
public static class IPEndPointHelper
{
    public static IPEndPoint Parse(string endPointString)
    {
        return Parse(endPointString, -1);
    }

    public static IPEndPoint Parse(string endPointString, int defaultPort)
    {
        if (string.IsNullOrEmpty(endPointString) || endPointString.Trim().Length == 0)
        {
            throw new ArgumentException("Endpoint descriptor may not be empty.");
        }

        if (defaultPort != -1 && (defaultPort < IPEndPoint.MinPort || defaultPort > IPEndPoint.MaxPort))
        {
            throw new ArgumentException(string.Format("Invalid default port '{0}'", defaultPort));
        }

        string[] values = endPointString.Split(new char[] { ':' });
        IPAddress ipaddy;
        int port = -1;

        // check if we have an IPv6 or ports
        if (values.Length <= 2)
        { // ipv4 or hostname
            if (values.Length == 1)
                // no port is specified, default
                port = defaultPort;
            else
                port = GetPort(values[1]);

            // try to use the address as IPv4, otherwise get hostname
            if (!IPAddress.TryParse(values[0], out ipaddy))
                ipaddy = GetIPFromHost(values[0]);
        }
        else if (values.Length > 2)
        { // ipv6
            // could [a:b:c]:d
            if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
            {
                string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
                ipaddy = IPAddress.Parse(ipaddressstring);
                port = GetPort(values[values.Length - 1]);
            }
            else
            { // [a:b:c] or a:b:c
                ipaddy = IPAddress.Parse(endPointString);
                port = defaultPort;
            }
        }
        else
        {
            throw new FormatException(string.Format("Invalid endpoint ipaddress '{0}'", endPointString));
        }

        if (port == -1)
            throw new ArgumentException(string.Format("No port specified: '{0}'", endPointString));

        return new IPEndPoint(ipaddy, port);
    }

    private static int GetPort(string p)
    {
        int port;

        if (!int.TryParse(p, out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
        {
            throw new FormatException(string.Format("Invalid end point port '{0}'", p));
        }

        return port;
    }

    private static IPAddress GetIPFromHost(string p)
    {
        var hosts = Dns.GetHostAddresses(p);

        if (hosts == null || hosts.Length == 0)
            throw new ArgumentException(string.Format("Host not found: {0}", p));

        return hosts[0];
    }
}
