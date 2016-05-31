using UnityEngine;

namespace Akka.Interfaced.SlimSocket.Client
{
    public class SlimTaskFactory : ISlimTaskFactory
    {
        internal MonoBehaviour Owner { get; set; }

        public ISlimTaskCompletionSource<TResult> Create<TResult>()
        {
            return new SlimTaskCompletionSource<TResult> { Owner = Owner };
        }
    }
}
