using System;
using System.Collections;
using System.Net;
using System.Threading;
using Akka.Interfaced;
using UnityEngine;

class SlimTask : Task
{
    internal MonoBehaviour Owner { get; set; }
    
    private Exception _exception;

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
        get; internal set;
    }

    public Exception Exception
    {
        get { return _exception; }
        set
        {
            if (IsCompleted)
                throw new InvalidOperationException("Already completed. status=" + Status);

            _exception = value;
            Status = TaskStatus.Faulted;
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
}

class SlimTask<T> : SlimTask, Task<T>
{
    private T _result;

    public T Result
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
}
