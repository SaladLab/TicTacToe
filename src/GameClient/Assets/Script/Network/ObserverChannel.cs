using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Interfaced;
using UnityEngine;

public class ObserverChannel
{
    private readonly List<IInterfacedObserver> _observers = new List<IInterfacedObserver>();

    private bool _isPending;
    private List<IInvokable> _pendingMessages = new List<IInvokable>();

    private bool _isKeepingOrder;
    private int _lastNotificationId;
    private List<Tuple<int, IInvokable>> _outOfOrderQueue;

    public ObserverChannel()
    {
    }

    public ObserverChannel(IInterfacedObserver observer, bool startPending = false, bool keepOrder = false)
    {
        Attach(observer);
        Pending = startPending;
        KeepingOrder = keepOrder;
    }

    public void Attach(IInterfacedObserver observer)
    {
        _observers.Add(observer);
    }

    public bool Detach(IInterfacedObserver observer)
    {
        return _observers.Remove(observer);
    }

    public bool Pending
    {
        get { return _isPending; }
        set
        {
            // When off, flush pending messages
            if (_isPending && value == false)
            {
                FlushPendingMessage();
            }

            _isPending = value;
        }
    }

    public bool KeepingOrder
    {
        get { return _isKeepingOrder; }
        set
        {
            // When on, initialize queue
            if (_isKeepingOrder == false && value)
            {
                _lastNotificationId = 0;
                _outOfOrderQueue = new List<Tuple<int, IInvokable>>();
            }

            // When off, flush pending messages
            if (_isKeepingOrder && value == false)
            {
                if (_outOfOrderQueue != null)
                {
                    foreach (var message in _outOfOrderQueue)
                        InvokeInternal(message.Item2);
                    _outOfOrderQueue.Clear();
                }
            }

            _isKeepingOrder = value;
        }
    }

    public void Invoke(int notificationId, IInvokable message)
    {
        G.Logger.Trace("Observer: " + message.GetType().Name);

        // Keep processing order

        if (_isKeepingOrder)
        {
            var expectedNotification = _lastNotificationId + 1;
            if (expectedNotification <= 0)
                expectedNotification = 1;

            if (expectedNotification != notificationId)
            {
                Debug.Log("!!! Hit !!!" + notificationId + " " + expectedNotification);

                // keep outOfOrderQueue in order
                var fi = _outOfOrderQueue.FindIndex(i => notificationId < i.Item1);
                if (fi != -1)
                    _outOfOrderQueue.Insert(fi, Tuple.Create(notificationId, message));
                else
                    _outOfOrderQueue.Add(Tuple.Create(notificationId, message));
                return;
            }

            _lastNotificationId = expectedNotification;
        }

        // Process message

        InvokeInternal(message);

        // Check outOfOrderQueue if we can process further messages in order

        if (_isKeepingOrder)
        {
            while (_outOfOrderQueue.Count > 0)
            {
                var expectedId = _lastNotificationId + 1;
                if (expectedId <= 0)
                    expectedId = 1;

                var item = _outOfOrderQueue[0];
                if (expectedId != item.Item1)
                    break;

                _outOfOrderQueue.RemoveAt(0);
                _lastNotificationId = expectedId;

                InvokeInternal(item.Item2);
            }
        }
    }

    private void InvokeInternal(IInvokable message)
    {
        if (_isPending)
        {
            if (_pendingMessages == null)
                _pendingMessages = new List<IInvokable>();
            _pendingMessages.Add(message);
        }
        else
        {
            foreach (var observer in _observers)
                message.Invoke(observer);
        }
    }

    private void FlushPendingMessage()
    {
        List<IInvokable> pendingMessages = _pendingMessages;
        if (pendingMessages == null || pendingMessages.Count == 0)
            return;

        _pendingMessages = null;
        foreach (var message in pendingMessages)
        {
            foreach (var observer in _observers)
                message.Invoke(observer);
        }
    }
}
