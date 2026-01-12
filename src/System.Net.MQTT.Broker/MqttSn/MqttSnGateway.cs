using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.MQTT.Broker.Transport;
using System.Net.MQTT.Broker.Transport.Udp;
using System.Net.MQTT.MqttSn.Protocol;
using System.Net.MQTT.MqttSn.Protocol.Packets;
using System.Net.MQTT.MqttSn.Serialization;
using System.Net.MQTT.MqttSn.TopicRegistry;
using System.Net.MQTT.Serialization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace System.Net.MQTT.Broker.MqttSn;

/// <summary>
/// MQTT-SN 网关。
/// 作为 MQTT-SN 客户端和 MQTT Broker 之间的桥梁。
/// </summary>
public sealed class MqttSnGateway : IAsyncDisposable
{
    private readonly MqttBroker _broker;
    private readonly MqttBrokerOptions _options;
    private readonly ILogger<MqttSnGateway>? _logger;
    private readonly ConcurrentDictionary<string, MqttSnClientSession> _sessions = new();
    private readonly MqttSnTopicRegistry _topicRegistry = new();

    private UdpTransportListener? _udpListener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private Task? _advertiseTask;
    private volatile bool _disposed;

    /// <summary>
    /// 网关 ID。
    /// </summary>
    public byte GatewayId { get; set; } = 1;

    /// <summary>
    /// 获取当前活跃会话数。
    /// </summary>
    public int ActiveSessions => _sessions.Count;

    /// <summary>
    /// 创建 MQTT-SN 网关。
    /// </summary>
    /// <param name="broker">MQTT Broker 实例</param>
    /// <param name="options">配置选项</param>
    /// <param name="logger">日志记录器</param>
    public MqttSnGateway(MqttBroker broker, MqttBrokerOptions options, ILogger<MqttSnGateway>? logger = null)
    {
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// 启动网关。
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_cts != null)
        {
            return; // 已启动
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 创建并启动 UDP 监听器
        var bindAddress = IPAddress.Parse(_options.BindAddress);
        _udpListener = new UdpTransportListener(bindAddress, _options.MqttSnPort)
        {
            ConnectionTimeout = TimeSpan.FromSeconds(_options.MqttSnSessionTimeoutSeconds)
        };
        _udpListener.Start();

        _logger?.LogInformation("MQTT-SN 网关已启动，端口: {Port}", _options.MqttSnPort);

        // 启动接受连接任务
        _acceptTask = AcceptClientsAsync(_cts.Token);

        // 启动网关广播任务
        if (_options.GatewayAdvertiseIntervalSeconds > 0)
        {
            _advertiseTask = AdvertiseLoopAsync(_cts.Token);
        }
    }

    /// <summary>
    /// 停止网关。
    /// </summary>
    public async Task StopAsync()
    {
        if (_cts == null)
        {
            return;
        }

        _cts.Cancel();

        // 断开所有客户端
        foreach (var session in _sessions.Values)
        {
            await DisconnectClientAsync(session);
        }
        _sessions.Clear();

        // 停止监听器
        if (_udpListener != null)
        {
            _udpListener.Stop();
            await _udpListener.DisposeAsync();
            _udpListener = null;
        }

        // 等待任务完成
        var tasks = new List<Task>();
        if (_acceptTask != null) tasks.Add(_acceptTask);
        if (_advertiseTask != null) tasks.Add(_advertiseTask);

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // 预期的取消
        }

        _cts.Dispose();
        _cts = null;

        _logger?.LogInformation("MQTT-SN 网关已停止");
    }

    /// <summary>
    /// 接受客户端连接的主循环。
    /// </summary>
    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _udpListener != null)
        {
            try
            {
                var connection = await _udpListener.AcceptAsync(cancellationToken);
                _ = HandleClientAsync(connection, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "接受 MQTT-SN 客户端连接时发生错误");
            }
        }
    }

    /// <summary>
    /// 处理客户端连接。
    /// </summary>
    private async Task HandleClientAsync(ITransportConnection connection, CancellationToken cancellationToken)
    {
        MqttSnClientSession? session = null;

        try
        {
            while (!cancellationToken.IsCancellationRequested && connection.IsConnected)
            {
                var datagram = await connection.ReadDatagramAsync(cancellationToken);
                if (datagram.IsEmpty)
                {
                    break;
                }

                var packet = MqttSnSerializer.Deserialize(datagram.Span);
                session = await ProcessPacketAsync(connection, packet, session, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "处理 MQTT-SN 客户端时发生错误: {Endpoint}", connection.RemoteEndPoint);
        }
        finally
        {
            if (session != null)
            {
                await HandleClientDisconnectAsync(session);
            }
        }
    }

    /// <summary>
    /// 处理接收到的报文。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task<MqttSnClientSession?> ProcessPacketAsync(
        ITransportConnection connection,
        IMqttSnPacket packet,
        MqttSnClientSession? session,
        CancellationToken cancellationToken)
    {
        switch (packet)
        {
            case MqttSnSearchGwPacket searchGw:
                await HandleSearchGwAsync(connection, searchGw, cancellationToken);
                break;

            case MqttSnConnectPacket connect:
                session = await HandleConnectAsync(connection, connect, cancellationToken);
                break;

            case MqttSnDisconnectPacket disconnect when session != null:
                await HandleDisconnectAsync(session, disconnect, cancellationToken);
                if (!disconnect.Duration.HasValue)
                {
                    session = null;
                }
                break;

            case MqttSnRegisterPacket register when session != null:
                await HandleRegisterAsync(session, register, cancellationToken);
                break;

            case MqttSnPublishPacket publish when session != null:
                await HandlePublishAsync(session, publish, cancellationToken);
                break;

            case MqttSnPubAckPacket pubAck when session != null:
                // 处理 PUBACK
                session.UpdateActivity();
                break;

            case MqttSnSubscribePacket subscribe when session != null:
                await HandleSubscribeAsync(session, subscribe, cancellationToken);
                break;

            case MqttSnUnsubscribePacket unsubscribe when session != null:
                await HandleUnsubscribeAsync(session, unsubscribe, cancellationToken);
                break;

            case MqttSnPingReqPacket pingReq:
                await HandlePingReqAsync(connection, session, pingReq, cancellationToken);
                break;

            case MqttSnWillTopicPacket willTopic when session != null:
                HandleWillTopic(session, willTopic);
                break;

            case MqttSnWillMsgPacket willMsg when session != null:
                HandleWillMsg(session, willMsg);
                break;

            default:
                _logger?.LogDebug("收到未处理的报文类型: {PacketType}", packet.PacketType);
                break;
        }

        return session;
    }

    /// <summary>
    /// 处理 SEARCHGW 请求。
    /// </summary>
    private async Task HandleSearchGwAsync(
        ITransportConnection connection,
        MqttSnSearchGwPacket packet,
        CancellationToken cancellationToken)
    {
        _logger?.LogDebug("收到 SEARCHGW 请求: Radius={Radius}", packet.Radius);

        var gwInfo = new MqttSnGwInfoPacket
        {
            GatewayId = GatewayId
        };

        await SendPacketAsync(connection, gwInfo, cancellationToken);
    }

    /// <summary>
    /// 处理 CONNECT 请求。
    /// </summary>
    private async Task<MqttSnClientSession> HandleConnectAsync(
        ITransportConnection connection,
        MqttSnConnectPacket packet,
        CancellationToken cancellationToken)
    {
        _logger?.LogDebug("收到 CONNECT 请求: ClientId={ClientId}, CleanSession={CleanSession}",
            packet.ClientId, packet.CleanSession);

        // 创建或获取会话
        var session = _sessions.GetOrAdd(packet.ClientId, _ => new MqttSnClientSession
        {
            ClientId = packet.ClientId
        });

        // 更新会话信息
        session.Transport = connection;
        session.RemoteEndPoint = connection.RemoteEndPoint;
        session.CleanSession = packet.CleanSession;
        session.KeepAliveSeconds = packet.Duration;
        session.State = MqttSnClientState.Active;
        session.ConnectedAt = DateTime.UtcNow;
        session.UpdateActivity();

        if (packet.CleanSession)
        {
            session.Subscriptions.Clear();
            session.SubscriptionQos.Clear();
        }

        // 如果有遗嘱标志，请求遗嘱主题
        if (packet.Will)
        {
            await SendPacketAsync(connection, MqttSnWillTopicReqPacket.Instance, cancellationToken);
        }
        else
        {
            // 发送 CONNACK
            var connAck = new MqttSnConnAckPacket
            {
                ReturnCode = MqttSnReturnCode.Accepted
            };
            await SendPacketAsync(connection, connAck, cancellationToken);
        }

        _logger?.LogInformation("MQTT-SN 客户端已连接: {ClientId}", packet.ClientId);
        return session;
    }

    /// <summary>
    /// 处理 DISCONNECT 请求。
    /// </summary>
    private async Task HandleDisconnectAsync(
        MqttSnClientSession session,
        MqttSnDisconnectPacket packet,
        CancellationToken cancellationToken)
    {
        if (packet.Duration.HasValue)
        {
            // 进入睡眠状态
            session.State = MqttSnClientState.Asleep;
            session.SleepDuration = packet.Duration.Value;
            _logger?.LogDebug("客户端进入睡眠状态: {ClientId}, Duration={Duration}s",
                session.ClientId, packet.Duration.Value);
        }
        else
        {
            // 完全断开
            session.State = MqttSnClientState.Disconnected;
            _logger?.LogInformation("MQTT-SN 客户端已断开: {ClientId}", session.ClientId);
        }

        // 发送 DISCONNECT 响应
        var response = new MqttSnDisconnectPacket();
        await SendPacketAsync(session.Transport!, response, cancellationToken);
    }

    /// <summary>
    /// 处理 REGISTER 请求。
    /// </summary>
    private async Task HandleRegisterAsync(
        MqttSnClientSession session,
        MqttSnRegisterPacket packet,
        CancellationToken cancellationToken)
    {
        session.UpdateActivity();

        // 注册主题
        var topicId = session.RegisterTopic(packet.TopicName);

        var regAck = new MqttSnRegAckPacket
        {
            TopicId = topicId,
            MessageId = packet.MessageId,
            ReturnCode = MqttSnReturnCode.Accepted
        };

        await SendPacketAsync(session.Transport!, regAck, cancellationToken);

        _logger?.LogDebug("主题已注册: {ClientId}, Topic={Topic}, TopicId={TopicId}",
            session.ClientId, packet.TopicName, topicId);
    }

    /// <summary>
    /// 处理 PUBLISH 请求。
    /// </summary>
    private async Task HandlePublishAsync(
        MqttSnClientSession session,
        MqttSnPublishPacket packet,
        CancellationToken cancellationToken)
    {
        session.UpdateActivity();

        // 解析主题
        string? topic = null;
        switch (packet.Flags.TopicType)
        {
            case MqttSnTopicType.Normal:
                topic = session.GetTopic(packet.TopicId);
                break;
            case MqttSnTopicType.ShortName:
                topic = packet.GetShortTopicName();
                break;
            case MqttSnTopicType.Predefined:
                topic = _topicRegistry.GetPredefinedTopic(packet.TopicId);
                break;
        }

        if (string.IsNullOrEmpty(topic))
        {
            // 主题 ID 无效
            if (packet.Flags.QoS != MqttQualityOfService.AtMostOnce)
            {
                var pubAck = new MqttSnPubAckPacket
                {
                    TopicId = packet.TopicId,
                    MessageId = packet.MessageId,
                    ReturnCode = MqttSnReturnCode.InvalidTopicId
                };
                await SendPacketAsync(session.Transport!, pubAck, cancellationToken);
            }
            return;
        }

        // 检查消息大小
        if (packet.Data.Length > _broker.Options.MaxMessageSize)
        {
            // 消息过大
            if (packet.Flags.QoS != MqttQualityOfService.AtMostOnce)
            {
                var pubAck = new MqttSnPubAckPacket
                {
                    TopicId = packet.TopicId,
                    MessageId = packet.MessageId,
                    ReturnCode = MqttSnReturnCode.NotSupported // 不支持（消息过大）
                };
                await SendPacketAsync(session.Transport!, pubAck, cancellationToken);
            }
            return;
        }

        // 创建 MQTT 消息并发布到 Broker
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = packet.Data,
            QualityOfService = packet.Flags.QoS,
            Retain = packet.Flags.Retain
        };

        await _broker.PublishAsync(message, MqttProtocolType.MqttSn, session.ClientId, cancellationToken);

        // 发送 PUBACK (QoS 1+)
        if (packet.Flags.QoS != MqttQualityOfService.AtMostOnce)
        {
            var pubAck = new MqttSnPubAckPacket
            {
                TopicId = packet.TopicId,
                MessageId = packet.MessageId,
                ReturnCode = MqttSnReturnCode.Accepted
            };
            await SendPacketAsync(session.Transport!, pubAck, cancellationToken);
        }

        _logger?.LogDebug("消息已发布: {ClientId}, Topic={Topic}, QoS={QoS}",
            session.ClientId, topic, packet.Flags.QoS);
    }

    /// <summary>
    /// 处理 SUBSCRIBE 请求。
    /// </summary>
    private async Task HandleSubscribeAsync(
        MqttSnClientSession session,
        MqttSnSubscribePacket packet,
        CancellationToken cancellationToken)
    {
        session.UpdateActivity();

        // 解析主题
        string? topic = null;
        ushort topicId = 0;

        switch (packet.Flags.TopicType)
        {
            case MqttSnTopicType.Normal:
                topic = packet.TopicName;
                if (!string.IsNullOrEmpty(topic))
                {
                    topicId = session.RegisterTopic(topic);
                }
                break;
            case MqttSnTopicType.ShortName:
                topic = new string(new[]
                {
                    (char)(packet.TopicId >> 8),
                    (char)(packet.TopicId & 0xFF)
                });
                topicId = packet.TopicId;
                break;
            case MqttSnTopicType.Predefined:
                topic = _topicRegistry.GetPredefinedTopic(packet.TopicId);
                topicId = packet.TopicId;
                break;
        }

        MqttSnReturnCode returnCode;
        MqttQualityOfService grantedQoS;

        if (string.IsNullOrEmpty(topic))
        {
            returnCode = MqttSnReturnCode.InvalidTopicId;
            grantedQoS = MqttQualityOfService.AtMostOnce;
        }
        else
        {
            // 添加订阅
            session.Subscriptions.Add(topic);
            session.SubscriptionQos[topic] = packet.Flags.QoS;

            // 订阅 Broker
            await _broker.SubscribeAsync(session.ClientId, topic, packet.Flags.QoS, cancellationToken);

            returnCode = MqttSnReturnCode.Accepted;
            grantedQoS = packet.Flags.QoS;

            _logger?.LogDebug("订阅成功: {ClientId}, Topic={Topic}, QoS={QoS}",
                session.ClientId, topic, grantedQoS);
        }

        // 发送 SUBACK
        var subAck = new MqttSnSubAckPacket
        {
            Flags = MqttSnFlags.Create().WithQoS(grantedQoS).Build(),
            TopicId = topicId,
            MessageId = packet.MessageId,
            ReturnCode = returnCode
        };
        await SendPacketAsync(session.Transport!, subAck, cancellationToken);
    }

    /// <summary>
    /// 处理 UNSUBSCRIBE 请求。
    /// </summary>
    private async Task HandleUnsubscribeAsync(
        MqttSnClientSession session,
        MqttSnUnsubscribePacket packet,
        CancellationToken cancellationToken)
    {
        session.UpdateActivity();

        // 解析主题
        string? topic = null;
        switch (packet.Flags.TopicType)
        {
            case MqttSnTopicType.Normal:
                topic = packet.TopicName;
                break;
            case MqttSnTopicType.ShortName:
                topic = new string(new[]
                {
                    (char)(packet.TopicId >> 8),
                    (char)(packet.TopicId & 0xFF)
                });
                break;
            case MqttSnTopicType.Predefined:
                topic = _topicRegistry.GetPredefinedTopic(packet.TopicId);
                break;
        }

        if (!string.IsNullOrEmpty(topic))
        {
            session.Subscriptions.Remove(topic);
            session.SubscriptionQos.Remove(topic);

            // 从 Broker 取消订阅
            await _broker.UnsubscribeAsync(session.ClientId, topic, cancellationToken);

            _logger?.LogDebug("取消订阅: {ClientId}, Topic={Topic}", session.ClientId, topic);
        }

        // 发送 UNSUBACK
        var unsubAck = new MqttSnUnsubAckPacket
        {
            MessageId = packet.MessageId
        };
        await SendPacketAsync(session.Transport!, unsubAck, cancellationToken);
    }

    /// <summary>
    /// 处理 PINGREQ 请求。
    /// </summary>
    private async Task HandlePingReqAsync(
        ITransportConnection connection,
        MqttSnClientSession? session,
        MqttSnPingReqPacket packet,
        CancellationToken cancellationToken)
    {
        // 如果有 ClientId，说明是睡眠客户端唤醒
        if (!string.IsNullOrEmpty(packet.ClientId))
        {
            if (_sessions.TryGetValue(packet.ClientId, out var sleepingSession))
            {
                sleepingSession.State = MqttSnClientState.Awake;
                sleepingSession.Transport = connection;
                sleepingSession.UpdateActivity();

                // 发送缓存的消息
                while (sleepingSession.SleepBuffer.TryDequeue(out var message))
                {
                    await DeliverMessageToClientAsync(sleepingSession, message, cancellationToken);
                }

                // 发送 PINGRESP 表示消息发送完毕
            }
        }
        else
        {
            session?.UpdateActivity();
        }

        await SendPacketAsync(connection, MqttSnPingRespPacket.Instance, cancellationToken);
    }

    /// <summary>
    /// 处理遗嘱主题。
    /// </summary>
    private void HandleWillTopic(MqttSnClientSession session, MqttSnWillTopicPacket packet)
    {
        session.WillTopic = packet.WillTopic;
        session.WillQoS = packet.Flags.QoS;
        session.WillRetain = packet.Flags.Retain;
        session.UpdateActivity();
    }

    /// <summary>
    /// 处理遗嘱消息。
    /// </summary>
    private void HandleWillMsg(MqttSnClientSession session, MqttSnWillMsgPacket packet)
    {
        session.WillMessage = packet.WillMessage;
        session.UpdateActivity();
    }

    /// <summary>
    /// 向客户端发送消息。
    /// </summary>
    public async Task DeliverMessageToClientAsync(
        MqttSnClientSession session,
        MqttApplicationMessage message,
        CancellationToken cancellationToken)
    {
        // 睡眠客户端缓存消息
        if (session.State == MqttSnClientState.Asleep)
        {
            if (session.SleepBuffer.Count < _options.SleepingClientBufferSize)
            {
                session.SleepBuffer.Enqueue(message);
            }
            return;
        }

        if (session.Transport == null || !session.Transport.IsConnected)
        {
            return;
        }

        // 获取或注册主题 ID
        var topicId = session.GetTopicId(message.Topic);
        if (!topicId.HasValue)
        {
            // 需要先注册主题
            topicId = session.RegisterTopic(message.Topic);

            var register = new MqttSnRegisterPacket
            {
                TopicId = topicId.Value,
                MessageId = session.GetNextMessageId(),
                TopicName = message.Topic
            };
            await SendPacketAsync(session.Transport, register, cancellationToken);
        }

        // 发送 PUBLISH
        var qos = session.SubscriptionQos.GetValueOrDefault(message.Topic, MqttQualityOfService.AtMostOnce);
        var publish = new MqttSnPublishPacket
        {
            Flags = MqttSnFlags.Create()
                .WithQoS(qos)
                .WithRetain(message.Retain)
                .WithTopicType(MqttSnTopicType.Normal)
                .Build(),
            TopicId = topicId.Value,
            MessageId = qos != MqttQualityOfService.AtMostOnce ? session.GetNextMessageId() : (ushort)0,
            Data = message.Payload.IsEmpty ? Array.Empty<byte>() : message.Payload.ToArray()
        };

        await SendPacketAsync(session.Transport, publish, cancellationToken);
    }

    /// <summary>
    /// 网关广播循环。
    /// </summary>
    private async Task AdvertiseLoopAsync(CancellationToken cancellationToken)
    {
        var advertise = new MqttSnAdvertisePacket
        {
            GatewayId = GatewayId,
            Duration = (ushort)_options.GatewayAdvertiseIntervalSeconds
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.GatewayAdvertiseIntervalSeconds), cancellationToken);

                // TODO: 广播到所有已知客户端
                // 这里需要实现广播机制
                _logger?.LogDebug("发送 ADVERTISE 广播");
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 处理客户端断开连接。
    /// </summary>
    private async Task HandleClientDisconnectAsync(MqttSnClientSession session)
    {
        if (session.State == MqttSnClientState.Asleep)
        {
            // 睡眠客户端不移除会话
            return;
        }

        // 发布遗嘱消息
        if (!string.IsNullOrEmpty(session.WillTopic) && session.WillMessage != null)
        {
            var willMessage = new MqttApplicationMessage
            {
                Topic = session.WillTopic,
                Payload = session.WillMessage,
                QualityOfService = session.WillQoS,
                Retain = session.WillRetain
            };
            await _broker.PublishAsync(willMessage, MqttProtocolType.MqttSn, session.ClientId);
        }

        // 清理会话
        if (session.CleanSession)
        {
            _sessions.TryRemove(session.ClientId, out _);
            _topicRegistry.ClearClientTopics(session.ClientId);
        }
    }

    /// <summary>
    /// 断开客户端连接。
    /// </summary>
    private async Task DisconnectClientAsync(MqttSnClientSession session)
    {
        if (session.Transport != null && session.Transport.IsConnected)
        {
            try
            {
                var disconnect = new MqttSnDisconnectPacket();
                await SendPacketAsync(session.Transport, disconnect, CancellationToken.None);
                await session.Transport.CloseAsync();
            }
            catch
            {
                // 忽略错误
            }
        }
    }

    /// <summary>
    /// 发送报文到连接。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task SendPacketAsync(
        ITransportConnection connection,
        IMqttSnPacket packet,
        CancellationToken cancellationToken)
    {
        var length = MqttSnSerializer.SerializeWithPooledBuffer(packet, out var buffer);
        try
        {
            await connection.WriteAsync(buffer.AsMemory(0, length), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            await StopAsync();
        }
    }

    /// <summary>
    /// 检查是否已释放。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttSnGateway));
        }
    }
}
