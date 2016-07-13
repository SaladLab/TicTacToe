using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocket;
using Akka.Interfaced.SlimSocket.Client;
using Common.Logging;
using Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoginProcessor
{
    public static Task Login(MonoBehaviour owner, IPEndPoint endPoint, string id, string password, Action<string> progressReport)
    {
        var task = new UnitySlimTaskCompletionSource<bool>();
        task.Owner = owner;
        owner.StartCoroutine(LoginCoroutine(endPoint, id, password, task, progressReport));
        return task;
    }

    private static IEnumerator LoginCoroutine(IPEndPoint endPoint, string id, string password,
                                              ISlimTaskCompletionSource<bool> tcs, Action<string> progressReport)
    {
        // Connect

        if (progressReport != null)
            progressReport("Connect");

        var communicator = UnityCommunicatorFactory.Create();
        {
            var channelFactory = communicator.ChannelFactory;
            channelFactory.Type = ChannelType.Udp;
            channelFactory.ConnectEndPoint = endPoint;
            channelFactory.CreateChannelLogger = () => LogManager.GetLogger("Channel");
            channelFactory.PacketSerializer = PacketSerializer.CreatePacketSerializer<DomainProtobufSerializer>();
        }

        var channel = communicator.CreateChannel();
        var t0 = channel.ConnectAsync();
        yield return t0.WaitHandle;
        if (t0.Exception != null)
        {
            tcs.TrySetException(new Exception("Connect error\n" + t0.Exception, t0.Exception));
            yield break;
        }

        G.Communicator = communicator;

        // Login

        if (progressReport != null)
            progressReport("Login");

        channel = G.Communicator.Channels[0];
        var userLogin = channel.CreateRef<UserLoginRef>();
        var t1 = userLogin.Login(id, password);
        yield return t1.WaitHandle;

        if (t1.Status != TaskStatus.RanToCompletion)
        {
            tcs.TrySetException(new Exception("Login Error\n" + t1.Exception, t1.Exception));
            yield break;
        }

        // Query User

        if (progressReport != null)
            progressReport("Query User");

        var userId = t1.Result.Item1;
        var userInitiator = (UserInitiatorRef)t1.Result.Item2;

        if (userInitiator.IsChannelConnected() == false)
        {
            channel.Close();
            var t2 = userInitiator.ConnectChannelAsync();
            yield return t2.WaitHandle;
            if (t2.Exception != null)
            {
                tcs.TrySetException(new Exception("ConnectToUser error\n" + t2.Exception, t2.Exception));
                yield break;
            }
        }

        var observer = communicator.ObserverRegistry.Create<IUserEventObserver>(UserEventProcessor.Instance, startPending: true);

        var t3 = userInitiator.Load(observer);
        yield return t3.WaitHandle;
        if (t3.Exception != null)
        {
            if (t3.Exception is ResultException && ((ResultException)t3.Exception).ResultCode == ResultCodeType.UserNeedToBeCreated)
            {
                var inputDialog = UiInputBox.Show("Input your name:");
                yield return inputDialog.WaitForHide();
                var userName = (string)inputDialog.ReturnValue;
                var t4 = userInitiator.Create(observer, userName);
                yield return t4.WaitHandle;
                if (t4.Exception != null)
                {
                    tcs.TrySetException(new Exception("CreateUser error\n" + t4.Exception, t4.Exception));
                    communicator.ObserverRegistry.Remove(observer);
                    yield break;
                }
                G.UserContext = t4.Result;
            }
            else
            {
                tcs.TrySetException(new Exception("LoadUser error\n" + t3.Exception, t3.Exception));
                communicator.ObserverRegistry.Remove(observer);
                yield break;
            }
        }
        else
        {
            G.UserContext = t3.Result;
        }

        G.User = userInitiator.Cast<UserRef>();
        G.UserId = userId;

        communicator.Channels.Last().StateChanged += (_, state) =>
        {
            if (state == ChannelStateType.Closed)
                ChannelEventDispatcher.Post(OnChannelClose, _);
        };

        tcs.TrySetResult(true);

        observer.GetEventDispatcher().Pending = false;
    }

    private static void OnChannelClose(object channel)
    {
        G.User = null;
        SceneManager.LoadScene("MainScene");
    }

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
}
