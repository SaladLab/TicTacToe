using System;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Domain.Interfaced;
using Akka.Interfaced.SlimSocket.Client;
using Akka.Interfaced.SlimSocket.Base;
using Common.Logging;
using TypeAlias;

public static class LoginProcessor 
{
    public static Task Login(IPEndPoint endPoint, string id, string password, Action<string> progressReport)
    {
        var task = new SlimTask<bool>();
        task.Owner = ApplicationComponent.Instance;
        ApplicationComponent.Instance.StartCoroutine(
            LoginCoroutine(endPoint, id, password, task, progressReport));
        return task;
    }

    private static IEnumerator LoginCoroutine(IPEndPoint endPoint, string id, string password, 
                                              SlimTask<bool> task, Action<string> progressReport)
    {
        // Connect

        if (progressReport != null)
            progressReport("Connect");

        if (G.Comm == null || G.Comm.State == Communicator.StateType.Stopped)
        {
            var serializer = new PacketSerializer(
                new PacketSerializerBase.Data(
                    new ProtoBufMessageSerializer(new DomainProtobufSerializer()),
                    new TypeAliasTable()));

            G.Comm = new Communicator(LogManager.GetLogger("Communicator"),
                                      endPoint,
                                      _ => new TcpConnection(serializer, LogManager.GetLogger("Connection")));
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

        var userLogin = new UserLoginRef(new SlimActorRef(1), G.SlimRequestWaiter, null);
        var observerId = G.Comm.IssueObserverId();
        var observer = new ObserverEventDispatcher(ApplicationComponent.Instance, true, true);
        G.Comm.AddObserver(observerId, observer);
        var t1 = userLogin.Login(id, password, observerId);
        yield return t1.WaitHandle;

        if (t1.Status != TaskStatus.RanToCompletion)
        {
            task.Exception = new Exception("Login Error\n" + t1.Exception, t1.Exception);
            G.Comm.RemoveObserver(observerId);
            yield break;
        }

        G.User = new UserRef(new SlimActorRef(t1.Result.UserActorBindId), G.SlimRequestWaiter, null);
        G.UserId = t1.Result.UserId;
        G.UserContext = t1.Result.UserContext;

        task.Status = TaskStatus.RanToCompletion;

        observer.Pending = false;
    }
}
