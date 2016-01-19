using System;
using UnityEngine;

namespace Akka.Interfaced.SlimSocket.Client
{
    public class SlimRequestWaiter : IRequestWaiter
    {
        public Communicator Communicator { get; private set; }
        public MonoBehaviour Owner { get; private set; }

        public SlimRequestWaiter(Communicator communicator, MonoBehaviour owner)
        {
            Communicator = communicator;
            Owner = owner;
        }

        void IRequestWaiter.SendRequest(IActorRef target, RequestMessage requestMessage)
        {
            Communicator.SendRequest(target, requestMessage);
        }

        Task IRequestWaiter.SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            var task = new SlimTask<bool> {Owner = Owner};
            Communicator.SendRequestAndWait(task, target, requestMessage, timeout);
            return task;
        }

        Task<T> IRequestWaiter.SendRequestAndReceive<T>(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
        {
            var task = new SlimTask<T> { Owner = Owner };
            Communicator.SendRequestAndReceive<T>(task, target, requestMessage, timeout);
            return task;
        }
    }
}
