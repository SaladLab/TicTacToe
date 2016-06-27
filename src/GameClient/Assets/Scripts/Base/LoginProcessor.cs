using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket.Client;
using Domain;
using UnityEngine;
using Common.Logging;
using Akka.Interfaced.SlimSocket;

public static class LoginProcessor
{
    public static IPEndPoint GetEndPointAddress(string address)
    {
        var a = address.Trim();

        // use deault if empty string

        if (string.IsNullOrEmpty(a))
        {
            return G.DefaultServerEndPoint;
        }

        // use 192.168.0.num if *.num when local ip address is 192.168.0.~

        if (a.StartsWith("*."))
        {
            var end = int.Parse(a.Substring(2));
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var abytes = ip.GetAddressBytes();
                    abytes[abytes.Length - 1] = (byte)end;
                    return new IPEndPoint(new IPAddress(abytes), G.DefaultServerEndPoint.Port);
                }
            }
        }

        return IPEndPointHelper.Parse(address, G.DefaultServerEndPoint.Port);
    }

    public static Task Login(MonoBehaviour owner, IPEndPoint endPoint, string id, string password, Action<string> progressReport)
    {
        var task = new UnitySlimTaskCompletionSource<bool>();
        task.Owner = owner;
        owner.StartCoroutine(LoginCoroutine(endPoint, id, password, task, progressReport));
        return task;
    }

    private static IEnumerator LoginCoroutine(IPEndPoint endPoint, string id, string password,
                                              ISlimTaskCompletionSource<bool> task, Action<string> progressReport)
    {
        // Connect

        if (progressReport != null)
            progressReport("Connect");

        if (G.Channel == null || G.Channel.State != ChannelStateType.Connected)
        {
            var channelFactory = ChannelFactoryBuilder.Build<DomainProtobufSerializer>(
                endPoint: endPoint,
                createChannelLogger: () => LogManager.GetLogger("Channel"));
            channelFactory.Type = ChannelType.Tcp;
            var channel = channelFactory.Create();

            // connect to gateway

            var t0 = channel.ConnectAsync();
            yield return t0.WaitHandle;
            if (t0.Exception != null)
            {
                task.TrySetException(new Exception("Failed to connect"));
                yield break;
            }
            G.Channel = channel;
        }

        // Login

        if (progressReport != null)
            progressReport("Login");

        var userLogin = G.Channel.CreateRef<UserLoginRef>();
        var observer = G.Channel.CreateObserver<IUserEventObserver>(UserEventProcessor.Instance, startPending: true);
        var t1 = userLogin.Login(id, password, observer);
        yield return t1.WaitHandle;

        if (t1.Status != TaskStatus.RanToCompletion)
        {
            G.Channel.RemoveObserver(observer);
            task.TrySetException(new Exception("Login Error\n" + t1.Exception, t1.Exception));
            yield break;
        }

        G.User = (UserRef)t1.Result.User;
        G.UserId = t1.Result.UserId;
        G.UserContext = t1.Result.UserContext;

        task.TrySetResult(true);

        observer.GetEventDispatcher().Pending = false;
    }
}
