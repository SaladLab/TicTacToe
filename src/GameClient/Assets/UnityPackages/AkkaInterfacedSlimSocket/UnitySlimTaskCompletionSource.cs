using System;
using System.Collections;
using UnityEngine;

namespace Akka.Interfaced.SlimSocket.Client
{
    public class UnitySlimTaskCompletionSource<TResult> : Task<TResult>, ISlimTaskCompletionSource<TResult>
    {
        internal MonoBehaviour Owner { get; set; }

        private Exception _exception;
        private TResult _result;

        // Handle for Unity Coroutine.
        // It may be used like as 'yield return task.WaitHandle'
        public object WaitHandle
        {
            get { return Owner.StartCoroutine(WaitForCompleted()); }
        }

        private IEnumerator WaitForCompleted()
        {
            while (IsCompleted == false)
                yield return null;
        }

        public TaskStatus Status
        {
            get; private set;
        }

        public Exception Exception
        {
            get
            {
                return _exception;
            }
            set
            {
                if (IsCompleted)
                    throw new InvalidOperationException("Already completed. status=" + Status);

                _exception = value;
                Status = TaskStatus.Faulted;
            }
        }

        public TResult Result
        {
            get
            {
                if (Status != TaskStatus.RanToCompletion)
                    throw new InvalidOperationException("Result is not set yet. status=" + Status);

                return _result;
            }

            set
            {
                if (IsCompleted)
                    throw new InvalidOperationException("Already completed. status=" + Status);

                _result = value;
                Status = TaskStatus.RanToCompletion;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Status == TaskStatus.RanToCompletion ||
                        Status == TaskStatus.Canceled ||
                        Status == TaskStatus.Faulted;
            }
        }

        public bool IsSucceeded
        {
            get { return Status == TaskStatus.RanToCompletion; }
        }

        public bool IsFailed
        {
            get
            {
                return Status == TaskStatus.Canceled ||
                        Status == TaskStatus.Faulted;
            }
        }

        public bool TrySetCanceled()
        {
            if (IsCompleted)
                return false;

            _exception = new OperationCanceledException();
            Status = TaskStatus.Canceled;
            return true;
        }

        public bool TrySetException(Exception e)
        {
            if (IsCompleted)
                return false;

            Exception = e;
            return true;
        }

        public bool TrySetResult(TResult result)
        {
            if (IsCompleted)
                return false;

            Result = result;
            return true;
        }

        public override string ToString()
        {
            if (Status == TaskStatus.RanToCompletion)
                return "Result: " + Result;
            if (Status == TaskStatus.Faulted)
                return "Faulted: " + Exception;
            if (Status == TaskStatus.Canceled)
                return "Canceled";

            return "Status: " + Status;
        }

        public Task<TResult> Task
        {
            get { return this; }
        }
    }
}
