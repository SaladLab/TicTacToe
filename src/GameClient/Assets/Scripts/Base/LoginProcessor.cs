using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket.Client;
using Domain.Interface;
using UnityEngine;

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

        // use 192.168.100.num if *.num when local ip address is 192.168.100.~

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
        var task = new SlimTaskCompletionSource<bool>();
        task.Owner = owner;
        owner.StartCoroutine(LoginCoroutine(endPoint, id, password, task, progressReport));
        return task;
    }

    private static IEnumerator LoginCoroutine(IPEndPoint endPoint, string id, string password,
                                              SlimTaskCompletionSource<bool> task, Action<string> progressReport)
    {
        // Connect

        if (progressReport != null)
            progressReport("Connect");

        if (G.Comm == null || G.Comm.State == Communicator.StateType.Stopped)
        {
            G.Comm = CommunicatorHelper.CreateCommunicator<DomainProtobufSerializer>(G.Logger, endPoint);
            G.Comm.Start();
        }

        while (true)
        {
            if (G.Comm.State == Communicator.StateType.Connected)
                break;

            if (G.Comm.State == Communicator.StateType.Stopped)
            {
                task.Exception = new Exception("Failed to connect");
                yield break;
            }

            yield return null;
        }

        // Login

        if (progressReport != null)
            progressReport("Login");

        var userLogin = G.Comm.CreateRef<UserLoginRef>();
        var observer = G.Comm.CreateObserver<IUserEventObserver>(UserEventProcessor.Instance, startPending: true);
        var t1 = userLogin.Login(id, password, observer);
        yield return t1.WaitHandle;

        if (t1.Status != TaskStatus.RanToCompletion)
        {
            observer.Dispose();
            task.Exception = new Exception("Login Error\n" + t1.Exception, t1.Exception);
            yield break;
        }

        G.User = (UserRef)t1.Result.User;
        G.UserId = t1.Result.UserId;
        G.UserContext = t1.Result.UserContext;

        task.Result = true;

        observer.GetEventDispatcher().Pending = false;
    }
}
