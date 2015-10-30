using System;
using System.Collections;
using System.Net;
using Akka.Interfaced;
using Domain.Interfaced;

public static class LoginProcessor 
{
    public static SlimTask Login(IPEndPoint endPoint, string id, string password, Action<string> progressReport)
    {
        var task = new SlimTask();
        task.Owner = ApplicationComponent.Instance;
        ApplicationComponent.Instance.StartCoroutine(
            LoginCoroutine(endPoint, id, password, task, progressReport));
        return task;
    }

    private static IEnumerator LoginCoroutine(IPEndPoint endPoint, string id, string password, 
                                              SlimTask task, Action<string> progressReport)
    {
        // Connect

        if (progressReport != null)
            progressReport("Connect");

        if (G.Comm == null || G.Comm.State == Communicator.StateType.Stopped)
        {
            G.Comm = new Communicator(G.Logger, ApplicationComponent.Instance);
            G.Comm.ServerEndPoint = endPoint;
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

        var userLogin = new UserLoginRef(new SlimActorRef { Id = 1 }, new SlimRequestWaiter { Communicator = G.Comm }, null);
        var observerId = G.Comm.IssueObserverId();
        var t1 = userLogin.Login(id, password, observerId);
        yield return t1.WaitHandle;

        if (t1.Status != TaskStatus.RanToCompletion)
        {
            task.Exception = new Exception("Login Error\n" + t1.Exception, t1.Exception);
            yield break;
        }

        G.Comm.AddObserver(observerId, new ObserverChannel(ApplicationComponent.Instance));
        G.User = new UserRef(new SlimActorRef { Id = t1.Result }, new SlimRequestWaiter { Communicator = G.Comm }, null);
        G.UserId = id; // TODO: need to get normalized id for using id as a key

        task.Status = TaskStatus.RanToCompletion;
    }
}
