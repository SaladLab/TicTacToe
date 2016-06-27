using UnityEngine;

namespace Akka.Interfaced.SlimSocket.Client
{
    public class UnitySlimTaskFactory : ISlimTaskFactory
    {
        internal MonoBehaviour Owner { get; set; }

        public ISlimTaskCompletionSource<TResult> Create<TResult>()
        {
            return new UnitySlimTaskCompletionSource<TResult> { Owner = Owner };
        }
    }
}
