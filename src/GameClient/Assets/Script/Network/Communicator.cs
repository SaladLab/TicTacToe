using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using Common.Logging;
using Akka.Interfaced;
using Akka.Interfaced.SlimSocketBase;
using Akka.Interfaced.SlimSocketClient;
using TypeAlias;
using UnityEngine;
using UnityEngine.Networking;
using Version = System.Version;

public class Communicator
{
    public IPEndPoint ServerEndPoint { get; set; }
    public StateType State { get { return _state; } }

    public enum StateType
    {
        None,
        Offline,
        Connecting,
        Connected,
        Paused,
        Stopped,
    }

    public enum StopReasonType
    {
        None,
        Explicit,
        ConnectClosed,
        ConnectTimeout,
        Expired,
        Kicked,
        Mismatched,
        InvalidVersion
    }

    public enum ReconnectActionType
    {
        None,
        StopWhenFail,
        PauseWhenFail,
    }

    private volatile StateType _state = StateType.None;
    private volatile StopReasonType _stopReason = StopReasonType.None;
    private int _stateRefValue;
    private DateTime _offlineStartTime = DateTime.MinValue;
    private DateTime _connectCoolTime = DateTime.MinValue;
    private DateTime _idleEchoSendTime;
    private readonly ILog _logger;
    private MonoBehaviour _owner;
    private TcpConnection _tcpConnection;
    private bool _closeTriggered;
    private ReconnectActionType _reconnectAction = ReconnectActionType.StopWhenFail;

    private readonly List<Packet> _recvSimplePackets = new List<Packet>();
    private readonly Dictionary<int, ObserverChannel> _observerMap = new Dictionary<int, ObserverChannel>();
    private int _lastObserverId;

    private static readonly TimeSpan kConnectTimeout = new TimeSpan(0, 0, 0, 10);
    private static readonly TimeSpan kIdleEchoTimeout = new TimeSpan(0, 0, 0, 30);

    public Communicator(ILog logger, MonoBehaviour owner)
    {
        _logger = logger;
        _owner = owner;
    }

    public void Start()
    {
        _logger.Info("Start");
        _state = StateType.Offline;
        _offlineStartTime = DateTime.UtcNow;
    }

    public void Stop()
    {
        _logger.Info("Stop");
        _state = StateType.Stopped;
        _stopReason = StopReasonType.Explicit;
        if (_tcpConnection != null)
            _tcpConnection.Close();
    }

    private void CreateNewConnect()
    {
        _closeTriggered = false;

        var serializer = new PacketSerializer(
            new PacketSerializerBase.Data(
                new ProtoBufMessageSerializer(new DomainProtobufSerializer()),
                new TypeAliasTable()));
        _tcpConnection = new TcpConnection(serializer, _logger);
        _tcpConnection.Connected += OnConnection;
        _tcpConnection.Received += OnRecvPacket;
        _tcpConnection.Closed += OnClose;
        _tcpConnection.Connect(ServerEndPoint);
    }

    public int IssueObserverId()
    {
        return ++_lastObserverId;
    }

    public void AddObserver(int observerId, ObserverChannel observer)
    {
        _observerMap.Add(observerId, observer);
    }

    public void RemoveObserver(int observerId)
    {
        _observerMap.Remove(observerId);
    }

    public ObserverChannel GetObserver(int observerId)
    {
        ObserverChannel observer;
        return _observerMap.TryGetValue(observerId, out observer)
                   ? observer
                   : null;
    }

    public void Update()
    {
        switch (_state)
        {
            case StateType.Offline:
                UpdateWhenOffline();
                break;

            case StateType.Connected:
                UpdateWhenConnected();
                break;
        }

        // 접속 끊어짐 지연 처리

        if (_closeTriggered)
        {
            if (_state != StateType.Stopped)
            {
                _state = StateType.Offline;
                if (_offlineStartTime == DateTime.MinValue)
                    _offlineStartTime = DateTime.UtcNow;
                _connectCoolTime = DateTime.UtcNow + new TimeSpan(0, 0, 1);
            }

            _closeTriggered = false;
        }
    }

    private void UpdateWhenOffline()
    {
        if (DateTime.UtcNow < _connectCoolTime)
            return;

        if ((DateTime.UtcNow - _offlineStartTime) > kConnectTimeout)
        {
            if (_reconnectAction == ReconnectActionType.StopWhenFail)
            {
                _logger.Trace("Connect timeout and make communicator stopped");
                _state = StateType.Stopped;
                _stopReason = StopReasonType.ConnectTimeout;
            }
            else if (_reconnectAction == ReconnectActionType.PauseWhenFail)
            {
                _logger.Trace("Connect timeout and make communicator paused");
                _state = StateType.Paused;
            }
            return;
        }

        _logger.Trace("Detect connection closed. Try reconnection.");
        _state = StateType.Connecting;

        lock (_recvSimplePackets)
        {
            _recvSimplePackets.Clear();
        }

        CreateNewConnect();
    }

    private void UpdateWhenConnected()
    {
        List<Packet> simplePackets = null;
        lock (_recvSimplePackets)
        {
            if (_recvSimplePackets.Any())
            {
                simplePackets = new List<Packet>();
                simplePackets.AddRange(_recvSimplePackets);
                _recvSimplePackets.Clear();
            }
        }

        if (_requestPackets.Any())
        {
            foreach (var packet in _requestPackets)
                _tcpConnection.SendPacket(packet);

            _requestPackets.Clear();
        }

        if (simplePackets != null)
        {
            foreach (var packet in simplePackets)
            {
                var msg = packet.Message;
                if (msg == null)
                {
                    _logger.Warn("SimplePacket has no message");
                    continue;
                }

                var observerId = packet.ActorId;
                var notificationId = packet.RequestId;
                ObserverChannel observer;
                if (_observerMap.TryGetValue(observerId, out observer))
                {
                    observer.Invoke(notificationId, (IInvokable)msg);
                    continue;
                }

                _logger.WarnFormat("Notification has no target. ObserverId={0} Message={1}",
                    observerId, msg.GetType().Name);
            }
        }

        // 서버에게 보낸 요청이 없으면 주기적으로 패킷 전송

        /*
        if (_idleEchoSendTime == DateTime.MinValue)
        {
            var lastTime = _tcpConnection.LastReceiveTime;
            var delta = lastTime != DateTime.MinValue ? DateTime.UtcNow - lastTime : new TimeSpan();
            if (delta >= kIdleEchoTimeout)
            {
                var p = new Packet
                {
                    Type = PacketType.Simple,
                    Message = new PcIdleEcho { Data = 1 }
                };
                _tcpConnection.SendPacket(p);
                _idleEchoSendTime = DateTime.UtcNow;
            }
        }
        else
        {
            var delta = DateTime.UtcNow - _idleEchoSendTime;
            if (delta >= kIdleEchoTimeout)
            {
                _logger.Trace("Echo timeout. Close connection.");
                _tcpConnection.Close();
            }
        }
        */
    }

    private void OnConnection(object sender)
    {
        _logger.Trace("Connection Connected.");

        _state = StateType.Connected;
        _offlineStartTime = DateTime.MinValue;
        _idleEchoSendTime = DateTime.MinValue;
    }

    private void OnRecvPacket(object sender, object packet)
    {
        var p = (Packet)packet;
        switch (p.Type)
        {
            case PacketType.Notification:
                lock (_recvSimplePackets)
                    _recvSimplePackets.Add(p);
                break;

            case PacketType.Response:
                Action<ResponseMessage> handler;
                if (_requestResponseMap.TryGetValue(p.RequestId, out handler))
                {
                    _requestResponseMap.Remove(p.RequestId);
                    handler(new ResponseMessage
                    {
                        RequestId = p.RequestId,
                        ReturnPayload = (IValueGetable)p.Message,
                        Exception = p.Exception
                    });
                }
                break;
        }
    }

    private void OnClose(object sender, int reason)
    {
        _logger.Trace("OnClose reason=" + reason);
        _closeTriggered = true;
    }

    internal void SendRequest(IActorRef target, RequestMessage requestMessage)
    {
        // TODO: This request doesn't need reply, so it's better to remove reply processing

        SendRequestPacket(new Packet
        {
            Type = PacketType.Request,
            ActorId = ((SlimActorRef)target).Id,
            Message = requestMessage.InvokePayload,
        }, null);
    }

    internal Task SendRequestAndWait(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
    {
        var t = new SlimTask();
        t.Owner = _owner;
        t.Status = TaskStatus.Running;
        SendRequestPacket(new Packet
        {
            Type = PacketType.Request,
            ActorId = ((SlimActorRef)target).Id,
            Message = requestMessage.InvokePayload,
        }, r =>
        {
            if (r.Exception != null)
                t.Exception = r.Exception;
            else
                t.Status = TaskStatus.RanToCompletion;
        });
        return t;
    }

    internal Task<T> SendRequestAndReceive<T>(IActorRef target, RequestMessage requestMessage, TimeSpan? timeout)
    {
        var t = new SlimTask<T>();
        t.Owner = _owner;
        t.Status = TaskStatus.Running;
        SendRequestPacket(new Packet
        {
            Type = PacketType.Request,
            ActorId = ((SlimActorRef)target).Id,
            Message = requestMessage.InvokePayload,
        }, r =>
        {
            if (r.Exception != null)
                t.Exception = r.Exception;
            else if (r.ReturnPayload != null)
                t.Result = (T)((IValueGetable)r.ReturnPayload).Value;
        });
        return t;
    }

    private int _lastRequestId = 0;
    private List<Packet> _requestPackets = new List<Packet>();
    private Dictionary<int, Action<ResponseMessage>> _requestResponseMap = new Dictionary<int, Action<ResponseMessage>>();

    private void SendRequestPacket(Packet packet, Action<ResponseMessage> completionHandler)
    {
        packet.RequestId = ++_lastRequestId;

        if (completionHandler != null)
            _requestResponseMap.Add(packet.RequestId, completionHandler);

        if (_state == StateType.Connected)
            _tcpConnection.SendPacket(packet);
        else
            _requestPackets.Add(packet);
    }
}
