using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.MQTT.Broker.Bridge;
using System.Net.MQTT.Broker.Cluster;
using System.Net.MQTT.Broker.Transport;
using System.Net.MQTT.Broker.Transport.Tcp;
using System.Net.MQTT.Protocol;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;

namespace System.Net.MQTT.Broker;

/// <summary>
/// MQTT Broker 服务器实现。
/// 支持 MQTT 3.1.1 协议，提供客户端管理、消息分发、主题订阅等功能。
/// </summary>
public sealed partial class MqttBroker : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// 默认缓冲区大小（4KB）
    /// </summary>
    private const int DefaultBufferSize = 4096;

    /// <summary>
    /// 最大剩余长度字节数（MQTT 协议规定最多 4 字节）
    /// </summary>
    private const int MaxRemainingLengthBytes = 4;

    private readonly List<ITransportListener> _transportListeners = new();
    private readonly List<Task> _acceptTasks = new();
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, MqttClientSession> _sessions = new();
    private readonly ConcurrentDictionary<string, MqttApplicationMessage> _retainedMessages = new();
    private readonly ConcurrentDictionary<string, MqttClientSession> _persistentSessions = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<MqttApplicationMessage>> _offlineMessages = new();
    private readonly Protocol.IMqttConnectPacketParser _connectPacketParser;
    private bool _disposed;
    private readonly MqttBrokerEventDispatcher _eventDispatcher;
    private readonly List<IMqttBridge> _bridges = new();
    private IMqttClusterNode? _cluster;

    /// <summary>
    /// 获取 Broker 配置选项。
    /// </summary>
    public MqttBrokerOptions Options { get; }

    /// <summary>
    /// 获取或设置身份验证器。
    /// </summary>
    public IMqttAuthenticator Authenticator { get; set; } = AllowAllAuthenticator.Instance;

    /// <summary>
    /// 获取或设置授权器。
    /// </summary>
    public IMqttAuthorizer Authorizer { get; set; } = AllowAllAuthorizer.Instance;

    /// <summary>
    /// 获取已连接的客户端数量。
    /// </summary>
    public int ConnectedClients => _sessions.Count;

    /// <summary>
    /// 获取所有已连接的会话。
    /// </summary>
    public IEnumerable<MqttClientSession> Sessions => _sessions.Values;

    /// <summary>
    /// 获取 Broker 是否正在运行。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 获取所有已配置的桥接。
    /// </summary>
    public IReadOnlyList<IMqttBridge> Bridges => _bridges;

    /// <summary>
    /// 获取集群节点（如果已启用）。
    /// </summary>
    public IMqttClusterNode? Cluster => _cluster;

    /// <summary>
    /// 获取所有保留消息。
    /// </summary>
    public IReadOnlyDictionary<string, MqttApplicationMessage> RetainedMessages => _retainedMessages;

    /// <summary>
    /// 当客户端连接时触发。
    /// </summary>
    public event EventHandler<MqttClientConnectedEventArgs>? ClientConnected;

    /// <summary>
    /// 当客户端断开连接时触发。
    /// </summary>
    public event EventHandler<MqttClientDisconnectedEventArgs>? ClientDisconnected;

    /// <summary>
    /// 当消息即将发布时触发（处理前）。
    /// 可通过设置 ProcessMessage = false 丢弃消息，或设置 SendAck = false 不发送确认。
    /// </summary>
    public event EventHandler<MqttMessagePublishingEventArgs>? MessagePublishing;

    /// <summary>
    /// 当消息发布完成时触发（处理后）。
    /// </summary>
    public event EventHandler<MqttMessagePublishedEventArgs>? MessagePublished;

    /// <summary>
    /// 当客户端即将订阅主题时触发（处理前）。
    /// 可通过设置每个订阅请求的 Accept = false 拒绝订阅。
    /// </summary>
    public event EventHandler<MqttClientSubscribingEventArgs>? ClientSubscribing;

    /// <summary>
    /// 当客户端订阅主题完成时触发（处理后）。
    /// </summary>
    public event EventHandler<MqttClientSubscribedEventArgs>? ClientSubscribed;

    /// <summary>
    /// 当客户端即将取消订阅时触发（处理前）。
    /// 可通过设置每个取消请求的 Accept = false 拒绝取消订阅。
    /// </summary>
    public event EventHandler<MqttClientUnsubscribingEventArgs>? ClientUnsubscribing;

    /// <summary>
    /// 当客户端取消订阅完成时触发（处理后）。
    /// </summary>
    public event EventHandler<MqttClientUnsubscribedEventArgs>? ClientUnsubscribed;

    /// <summary>
    /// 当消息没有匹配的订阅者时触发。
    /// </summary>
    public event EventHandler<MqttMessageNotDeliveredEventArgs>? MessageNotDelivered;

    /// <summary>
    /// 当消息成功投递给订阅者时触发。
    /// 每投递一个订阅者触发一次。
    /// </summary>
    public event EventHandler<MqttMessageDeliveredEventArgs>? MessageDelivered;

    /// <summary>
    /// 使用默认配置创建 MQTT Broker。
    /// </summary>
    public MqttBroker() : this(new MqttBrokerOptions())
    {
    }

    /// <summary>
    /// 使用指定配置创建 MQTT Broker。
    /// </summary>
    /// <param name="options">Broker 配置选项</param>
    /// <exception cref="ArgumentNullException">当 options 为 null 时抛出</exception>
    public MqttBroker(MqttBrokerOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _eventDispatcher = new MqttBrokerEventDispatcher(this);
        _connectPacketParser = new Protocol.MqttConnectPacketParser(options.MaxMessageSize);
    }

    /// <summary>
    /// 使用指定端口创建 MQTT Broker。
    /// </summary>
    /// <param name="port">监听端口</param>
    public MqttBroker(int port) : this(new MqttBrokerOptions { Port = port })
    {
    }

    /// <summary>
    /// 添加桥接到远程 Broker。
    /// 桥接将在 Broker 启动时自动连接。
    /// </summary>
    /// <param name="options">桥接配置选项</param>
    /// <returns>创建的桥接实例</returns>
    /// <exception cref="ArgumentNullException">当 options 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当 Broker 已启动时抛出</exception>
    public IMqttBridge AddBridge(MqttBridgeOptions options)
    {
        ThrowIfDisposed();

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (IsRunning)
        {
            throw new InvalidOperationException("无法在 Broker 运行时添加桥接。请在启动前配置桥接。");
        }

        var bridge = new MqttBridge(this, options);
        _bridges.Add(bridge);
        return bridge;
    }

    /// <summary>
    /// 启用集群功能。
    /// 集群将在 Broker 启动时自动连接到其他节点。
    /// </summary>
    /// <param name="options">集群配置选项</param>
    /// <returns>创建的集群节点实例</returns>
    /// <exception cref="ArgumentNullException">当 options 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">当 Broker 已启动或集群已启用时抛出</exception>
    public IMqttClusterNode EnableCluster(MqttClusterOptions options)
    {
        ThrowIfDisposed();

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (IsRunning)
        {
            throw new InvalidOperationException("无法在 Broker 运行时启用集群。请在启动前配置集群。");
        }

        if (_cluster != null)
        {
            throw new InvalidOperationException("集群已启用。");
        }

        _cluster = new MqttCluster(this, options);
        return _cluster;
    }

    /// <summary>
    /// 启动 Broker 服务器。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();

        // 解析绑定地址
        var bindAddress = IPAddress.Parse(Options.BindAddress);

        // 启动普通 TCP 监听
        var tcpListener = new TcpTransportListener(bindAddress, Options.Port);
        tcpListener.Start();
        _transportListeners.Add(tcpListener);
        _acceptTasks.Add(AcceptClientsAsync(tcpListener, _cts.Token));

        // 如果配置了 TLS，启动 TLS 监听
        if (Options.UseTls && Options.ServerCertificate != null)
        {
            var tlsListener = new TcpTransportListener(
                bindAddress,
                Options.TlsPort,
                useTls: true,
                serverCertificate: Options.ServerCertificate,
                requireClientCertificate: Options.RequireClientCertificate);
            tlsListener.Start();
            _transportListeners.Add(tlsListener);
            _acceptTasks.Add(AcceptClientsAsync(tlsListener, _cts.Token));
        }

        IsRunning = true;

        // 启动集群节点
        if (_cluster != null)
        {
            await _cluster.StartAsync(cancellationToken);
        }

        // 启动所有桥接
        foreach (var bridge in _bridges)
        {
            await bridge.StartAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 停止 Broker 服务器。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) return;

        // 停止所有桥接
        foreach (var bridge in _bridges)
        {
            try
            {
                await bridge.StopAsync(cancellationToken);
            }
            catch
            {
                // 忽略停止桥接时的错误
            }
        }

        // 停止集群节点
        if (_cluster != null)
        {
            try
            {
                await _cluster.StopAsync(cancellationToken);
            }
            catch
            {
                // 忽略停止集群时的错误
            }
        }

        // 取消所有操作
        _cts?.Cancel();

        // 停止所有传输监听器
        foreach (var listener in _transportListeners)
        {
            listener.Stop();
        }

        // 断开所有客户端
        foreach (var session in _sessions.Values)
        {
            await DisconnectClientAsync(session);
        }

        // 等待所有接受任务完成
        foreach (var task in _acceptTasks)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // 正常取消，忽略
            }
        }

        _acceptTasks.Clear();
        _transportListeners.Clear();

        IsRunning = false;
    }

    /// <summary>
    /// 发布消息到所有匹配的订阅者。
    /// </summary>
    /// <param name="message">要发布的消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>投递成功的客户端数量</returns>
    public Task<int> PublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken = default)
    {
        return PublishAsync(message, MqttProtocolType.Internal, null, cancellationToken);
    }

    /// <summary>
    /// 发布消息到所有匹配的订阅者（带协议来源信息）。
    /// </summary>
    /// <param name="message">要发布的消息</param>
    /// <param name="protocol">消息来源协议类型</param>
    /// <param name="sourceClientId">来源客户端标识（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>投递成功的客户端数量</returns>
    public async Task<int> PublishAsync(
        MqttApplicationMessage message,
        MqttProtocolType protocol,
        string? sourceClientId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // 1. 触发 MessagePublishing 事件（允许用户控制是否处理）
        var publishingArgs = new MqttMessagePublishingEventArgs
        {
            Session = null,
            Protocol = protocol,
            SourceClientId = sourceClientId,
            Message = message,
            ProcessMessage = true,
            SendAck = true,
            ReasonCode = 0x00
        };
        MessagePublishing?.Invoke(this, publishingArgs);

        // 2. 如果用户不处理消息，直接返回
        if (!publishingArgs.ProcessMessage)
        {
            return 0;
        }

        // 3. 设置消息来源信息
        message.SourceProtocol = protocol;
        message.SourceClientId = sourceClientId;
        message.PublishedAt = DateTime.UtcNow;

        // 4. 处理保留消息
        if (message.Retain && Options.EnableRetainedMessages)
        {
            if (message.Payload.Length == 0)
            {
                // 空载荷表示删除保留消息
                _retainedMessages.TryRemove(message.Topic, out _);
            }
            else
            {
                _retainedMessages[message.Topic] = message;
            }
        }

        // 4. 分发消息到订阅者
        var deliveredCount = await DistributeMessageAsync(message, sourceClientId, cancellationToken, null, protocol);

        // 5. 触发事件（异步通知）
        if (deliveredCount == 0)
        {
            // 无接收者事件
            _eventDispatcher.Dispatch(BrokerEventType.MessageNotDelivered,
                new MqttMessageNotDeliveredEventArgs
                {
                    Session = null,
                    Protocol = protocol,
                    SourceClientId = sourceClientId,
                    Message = message
                }, MessageNotDelivered);
        }

        // 触发 MessagePublished 完成事件
        _eventDispatcher.Dispatch(BrokerEventType.MessagePublished,
            new MqttMessagePublishedEventArgs
            {
                Session = null,
                Protocol = protocol,
                SourceClientId = sourceClientId,
                Message = message,
                DeliveredCount = deliveredCount
            }, MessagePublished);

        return deliveredCount;
    }

    /// <summary>
    /// 直接发送消息给指定客户端。
    /// 如果客户端在线则立即发送，否则存入离线消息队列。
    /// </summary>
    /// <param name="clientId">目标客户端 ID</param>
    /// <param name="message">要发送的消息</param>
    /// <param name="triggerEvent">是否触发 MessageDelivered 事件，默认为 true</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功发送（true=在线并发送，false=离线已存入队列）</returns>
    public async Task<bool> SendToClientAsync(
        string clientId,
        MqttApplicationMessage message,
        bool triggerEvent = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(clientId))
            throw new ArgumentNullException(nameof(clientId));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        // 检查客户端是否在线
        if (_sessions.TryGetValue(clientId, out var session) && session.IsConnected)
        {
            // 在线，直接发送
            await SendPublishAsync(session, message, cancellationToken);

            // 触发事件
            if (triggerEvent)
            {
                _eventDispatcher.Dispatch(BrokerEventType.MessageDelivered,
                    new MqttMessageDeliveredEventArgs
                    {
                        SourceSession = null,
                        Protocol = MqttProtocolType.Internal,
                        SourceClientId = null,
                        TargetSession = session,
                        Message = message
                    }, MessageDelivered);
            }

            return true;
        }
        else
        {
            // 离线，存入队列
            var queue = _offlineMessages.GetOrAdd(clientId, _ => new ConcurrentQueue<MqttApplicationMessage>());

            // 限制离线消息队列大小
            while (queue.Count >= Options.MaxOfflineMessagesPerClient)
            {
                queue.TryDequeue(out _); // 移除最旧的消息
            }

            queue.Enqueue(message);
            return false;
        }
    }

    /// <summary>
    /// 批量发送消息给多个客户端。
    /// </summary>
    /// <param name="clientIds">目标客户端 ID 列表</param>
    /// <param name="message">要发送的消息</param>
    /// <param name="triggerEvent">是否触发 MessageDelivered 事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功发送的客户端数量（在线的）</returns>
    public async Task<int> SendToClientsAsync(
        IEnumerable<string> clientIds,
        MqttApplicationMessage message,
        bool triggerEvent = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var sentCount = 0;
        foreach (var clientId in clientIds)
        {
            if (await SendToClientAsync(clientId, message, triggerEvent, cancellationToken))
            {
                sentCount++;
            }
        }
        return sentCount;
    }

    /// <summary>
    /// 获取客户端的离线消息队列。
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <returns>离线消息列表</returns>
    public IReadOnlyList<MqttApplicationMessage> GetOfflineMessages(string clientId)
    {
        ThrowIfDisposed();

        if (_offlineMessages.TryGetValue(clientId, out var queue))
        {
            return queue.ToArray();
        }
        return Array.Empty<MqttApplicationMessage>();
    }

    /// <summary>
    /// 清空客户端的离线消息队列。
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <returns>被清空的消息数量</returns>
    public int ClearOfflineMessages(string clientId)
    {
        ThrowIfDisposed();

        if (_offlineMessages.TryRemove(clientId, out var queue))
        {
            return queue.Count;
        }
        return 0;
    }

    /// <summary>
    /// 发送离线消息给客户端（客户端上线时调用）。
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="triggerEvent">是否触发 MessageDelivered 事件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送的消息数量</returns>
    public async Task<int> DeliverOfflineMessagesAsync(
        string clientId,
        bool triggerEvent = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!_offlineMessages.TryRemove(clientId, out var queue))
        {
            return 0;
        }

        if (!_sessions.TryGetValue(clientId, out var session) || !session.IsConnected)
        {
            // 客户端不在线，放回队列
            _offlineMessages.TryAdd(clientId, queue);
            return 0;
        }

        var count = 0;
        while (queue.TryDequeue(out var message))
        {
            await SendPublishAsync(session, message, cancellationToken);
            count++;

            if (triggerEvent)
            {
                _eventDispatcher.Dispatch(BrokerEventType.MessageDelivered,
                    new MqttMessageDeliveredEventArgs
                    {
                        SourceSession = null,
                        Protocol = MqttProtocolType.Internal,
                        SourceClientId = null,
                        TargetSession = session,
                        Message = message
                    }, MessageDelivered);
            }
        }

        return count;
    }

    /// <summary>
    /// 检查客户端是否在线。
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <returns>是否在线</returns>
    public bool IsClientOnline(string clientId)
    {
        ThrowIfDisposed();
        return _sessions.TryGetValue(clientId, out var session) && session.IsConnected;
    }

    /// <summary>
    /// 获取所有在线客户端 ID。
    /// </summary>
    /// <returns>在线客户端 ID 列表</returns>
    public IReadOnlyList<string> GetOnlineClients()
    {
        ThrowIfDisposed();
        return _sessions.Values
            .Where(s => s.IsConnected)
            .Select(s => s.ClientId)
            .ToList();
    }

    /// <summary>
    /// 获取客户端会话信息。
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <returns>会话信息，如果不存在则返回 null</returns>
    public MqttClientSession? GetClientSession(string clientId)
    {
        ThrowIfDisposed();
        return _sessions.TryGetValue(clientId, out var session) ? session : null;
    }

    /// <summary>
    /// 为外部客户端（如 MQTT-SN 网关）添加订阅。
    /// </summary>
    /// <param name="clientId">客户端标识符</param>
    /// <param name="topicFilter">主题过滤器</param>
    /// <param name="qos">服务质量等级</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SubscribeAsync(string clientId, string topicFilter, MqttQualityOfService qos, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.Subscriptions.Add(topicFilter);
            session.SubscriptionQos[topicFilter] = qos;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 为外部客户端（如 MQTT-SN 网关）移除订阅。
    /// </summary>
    /// <param name="clientId">客户端标识符</param>
    /// <param name="topicFilter">主题过滤器</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task UnsubscribeAsync(string clientId, string topicFilter, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_sessions.TryGetValue(clientId, out var session))
        {
            session.Subscriptions.Remove(topicFilter);
            session.SubscriptionQos.Remove(topicFilter);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 设置保留消息（用于集群/桥接同步）。
    /// 不会触发消息分发，仅更新保留消息存储。
    /// </summary>
    /// <param name="message">保留消息</param>
    public void SetRetainedMessage(MqttApplicationMessage message)
    {
        ThrowIfDisposed();

        if (!Options.EnableRetainedMessages) return;

        if (message.Payload.Length == 0)
        {
            _retainedMessages.TryRemove(message.Topic, out _);
        }
        else
        {
            _retainedMessages[message.Topic] = message;
        }
    }

    /// <summary>
    /// 批量设置保留消息（用于集群/桥接同步）。
    /// </summary>
    /// <param name="messages">保留消息列表</param>
    public void SetRetainedMessages(IEnumerable<MqttApplicationMessage> messages)
    {
        ThrowIfDisposed();

        if (!Options.EnableRetainedMessages) return;

        foreach (var message in messages)
        {
            if (message.Payload.Length == 0)
            {
                _retainedMessages.TryRemove(message.Topic, out _);
            }
            else
            {
                _retainedMessages[message.Topic] = message;
            }
        }
    }

    /// <summary>
    /// 获取指定主题的保留消息。
    /// </summary>
    /// <param name="topic">主题</param>
    /// <returns>保留消息，如果不存在返回 null</returns>
    public MqttApplicationMessage? GetRetainedMessage(string topic)
    {
        ThrowIfDisposed();

        if (!Options.EnableRetainedMessages) return null;

        return _retainedMessages.TryGetValue(topic, out var message) ? message : null;
    }

    /// <summary>
    /// 获取所有保留消息。
    /// </summary>
    /// <returns>保留消息集合</returns>
    public IReadOnlyCollection<MqttApplicationMessage> GetAllRetainedMessages()
    {
        ThrowIfDisposed();
        return _retainedMessages.Values.ToList();
    }

    /// <summary>
    /// 断开指定客户端的连接。
    /// </summary>
    /// <param name="clientId">客户端 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步操作的任务</returns>
    public async Task DisconnectClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(clientId, out var session))
        {
            await DisconnectClientAsync(session);
        }
    }

    /// <summary>
    /// 断开客户端会话的连接。
    /// </summary>
    /// <param name="session">客户端会话</param>
    private async Task DisconnectClientAsync(MqttClientSession session)
    {
        session.IsConnected = false;
        session.CancellationTokenSource?.Cancel();

        try
        {
            // 使用新的传输抽象关闭连接
            if (session.Transport != null)
            {
                await session.Transport.CloseAsync();
            }
        }
        catch
        {
            // 忽略清理时的异常
        }

        _sessions.TryRemove(session.ClientId, out _);
        session.SendLock.Dispose();
    }

    /// <summary>
    /// 接受客户端连接的循环。
    /// </summary>
    /// <param name="listener">传输监听器</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task AcceptClientsAsync(ITransportListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var connection = await listener.AcceptAsync(cancellationToken);

                // 检查连接数限制
                if (_sessions.Count >= Options.MaxConnections)
                {
                    await connection.DisposeAsync();
                    continue;
                }

                // 异步处理客户端，不等待
                _ = HandleClientAsync(connection, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                // 继续接受其他连接
            }
        }
    }

    /// <summary>
    /// 处理单个客户端连接。
    /// </summary>
    /// <param name="connection">传输连接</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task HandleClientAsync(ITransportConnection connection, CancellationToken cancellationToken)
    {
        MqttClientSession? session = null;

        try
        {
            // 处理 CONNECT 报文
            session = await ProcessConnectAsync(connection, cancellationToken);

            if (session == null)
            {
                await connection.DisposeAsync();
                return;
            }

            // 触发客户端连接事件（异步通知）
            _eventDispatcher.Dispatch(BrokerEventType.ClientConnected,
                new MqttClientConnectedEventArgs { Session = session }, ClientConnected);

            // 自动投递离线消息
            if (Options.AutoDeliverOfflineMessages)
            {
                await DeliverOfflineMessagesAsync(session.ClientId, triggerEvent: true, cancellationToken);
            }

            // 开始处理客户端报文
            await ProcessClientAsync(session, cancellationToken);
        }
        catch (Exception ex)
        {
            if (session != null)
            {
                await HandleClientDisconnectAsync(session, ex);
            }
            else
            {
                await connection.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// 处理客户端的 CONNECT 报文。
    /// 使用 ArrayPool 减少内存分配。
    /// </summary>
    /// <param name="connection">传输连接</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功时返回客户端会话，失败时返回 null</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task<MqttClientSession?> ProcessConnectAsync(ITransportConnection connection, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Options.ConnectionTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // 使用解析器解析 CONNECT 报文
        var connectResult = await _connectPacketParser.ParseAsync(connection, linkedCts.Token);
        if (connectResult == null)
        {
            // 解析失败（报文类型错误或报文过大）
            return null;
        }

        // 提取解析结果
        var protocolVersion = connectResult.ProtocolVersion;
        var clientId = connectResult.ClientId;
        var cleanSession = connectResult.CleanSession;
        var keepAlive = connectResult.KeepAlive;
        var willMessage = connectResult.WillMessage;
        var username = connectResult.Username;
        var password = connectResult.Password;

        // 检查是否允许匿名连接
        if (!Options.AllowAnonymous && string.IsNullOrEmpty(username))
        {
            await SendConnAckAsync(connection, MqttConnectResultCode.NotAuthorized, false, protocolVersion, linkedCts.Token);
            return null;
        }

        // 判断是否为安全连接
        var isSecure = connection.TransportType == TransportType.TcpTls;

        // 身份验证（使用完整的客户端信息上下文）
        var authContext = new MqttAuthenticationContext
        {
            ClientId = clientId,
            Username = username,
            Password = password,
            RemoteEndpoint = connection.RemoteEndPoint?.ToString(),
            ProtocolVersion = protocolVersion,
            CleanSession = cleanSession,
            KeepAliveSeconds = keepAlive,
            HasWillMessage = willMessage != null,
            WillTopic = willMessage?.Topic,
            WillQoS = willMessage?.QualityOfService,
            IsSecureConnection = isSecure
        };

        var authResult = await Authenticator.AuthenticateAsync(authContext, linkedCts.Token);
        if (!authResult.IsAuthenticated)
        {
            // 根据认证结果返回对应的错误码
            var resultCode = authResult.ReasonCode switch
            {
                0x85 => MqttConnectResultCode.IdentifierRejected,
                0x87 => MqttConnectResultCode.NotAuthorized,
                _ => MqttConnectResultCode.BadUsernameOrPassword
            };
            await SendConnAckAsync(connection, resultCode, false, protocolVersion, linkedCts.Token);
            return null;
        }

        var sessionPresent = false;

        // 断开已存在的相同客户端 ID 的连接
        if (_sessions.TryGetValue(clientId, out var existingSession))
        {
            await DisconnectClientAsync(existingSession);
        }

        // 检查是否有持久化会话
        if (!cleanSession && Options.EnablePersistentSessions && _persistentSessions.TryGetValue(clientId, out var persistentSession))
        {
            sessionPresent = true;
        }

        // 创建新会话（使用协议处理器工厂获取对应版本的处理器）
        var session = new MqttClientSession
        {
            ClientId = clientId,
            Transport = connection,
            Username = username,
            CleanSession = cleanSession,
            KeepAliveSeconds = keepAlive,
            WillMessage = willMessage,
            IsConnected = true,
            RemoteEndpoint = connection.RemoteEndPoint?.ToString(),
            ProtocolVersion = protocolVersion,
            ProtocolHandler = MqttProtocolHandlerFactory.GetHandler(protocolVersion),
            CancellationTokenSource = new CancellationTokenSource()
        };

        // 恢复持久化会话的订阅
        if (sessionPresent && _persistentSessions.TryGetValue(clientId, out persistentSession))
        {
            foreach (var sub in persistentSession.Subscriptions)
            {
                session.Subscriptions.Add(sub);
            }
            foreach (var kvp in persistentSession.SubscriptionQos)
            {
                session.SubscriptionQos[kvp.Key] = kvp.Value;
            }
        }

        // 注册会话
        _sessions[clientId] = session;

        // 发送 CONNACK 响应
        await SendConnAckAsync(connection, MqttConnectResultCode.Success, sessionPresent, protocolVersion, linkedCts.Token);

        return session;
    }

    /// <summary>
    /// 发送 CONNACK 响应报文。
    /// 根据协议版本发送正确格式的响应。
    /// </summary>
    /// <param name="connection">传输连接</param>
    /// <param name="resultCode">连接结果代码</param>
    /// <param name="sessionPresent">会话是否存在</param>
    /// <param name="protocolVersion">协议版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task SendConnAckAsync(ITransportConnection connection, MqttConnectResultCode resultCode, bool sessionPresent, MqttProtocolVersion protocolVersion, CancellationToken cancellationToken)
    {
        byte[] packet;

        if (protocolVersion == MqttProtocolVersion.V500)
        {
            // MQTT 5.0 CONNACK: 固定头部 + 剩余长度 + 确认标志 + 原因码 + 属性长度(0)
            packet = new byte[]
            {
                (byte)((int)MqttPacketType.ConnAck << 4), // 固定头部
                0x03,                                       // 剩余长度 = 3 (flags + reason + props length)
                (byte)(sessionPresent ? 0x01 : 0x00),      // 连接确认标志
                (byte)resultCode,                           // 原因码
                0x00                                        // 属性长度 = 0
            };
        }
        else
        {
            // MQTT 3.1.1 CONNACK: 固定头部 + 剩余长度 + 确认标志 + 返回码
            packet = new byte[]
            {
                (byte)((int)MqttPacketType.ConnAck << 4), // 固定头部
                0x02,                                       // 剩余长度 = 2
                (byte)(sessionPresent ? 0x01 : 0x00),      // 连接确认标志
                (byte)resultCode                            // 连接返回码
            };
        }

        await connection.WriteAsync(packet.AsMemory(), cancellationToken);
        await connection.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// 处理客户端报文的主循环。
    /// 使用 ArrayPool 减少内存分配。
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task ProcessClientAsync(MqttClientSession session, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, session.CancellationTokenSource!.Token);
        var token = linkedCts.Token;
        var transport = session.Transport!;

        // 从池中租借头部缓冲区
        var headerBuffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            while (!token.IsCancellationRequested && session.IsConnected)
            {
                try
                {
                    // 读取固定头部（1 字节）
                    var read = await transport.ReadAsync(headerBuffer.AsMemory(0, 1), token);
                    if (read == 0) break;

                    // 更新最后活动时间
                    session.LastActivity = DateTime.UtcNow;

                    // 解析报文类型和标志
                    var packetType = (MqttPacketType)(headerBuffer[0] >> 4);
                    var flags = headerBuffer[0] & 0x0F;

                    // 解码剩余长度
                    var remainingLength = await DecodeRemainingLengthAsync(transport, token);

                    // 从池中租借有效载荷缓冲区
                    var payload = ArrayPool<byte>.Shared.Rent(remainingLength > 0 ? remainingLength : 1);
                    try
                    {
                        if (remainingLength > 0)
                        {
                            await ReadExactlyAsync(transport, payload, 0, remainingLength, token);
                        }

                        // 处理报文
                        await HandlePacketAsync(session, packetType, flags, payload.AsMemory(0, remainingLength), token);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(payload);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }

        await HandleClientDisconnectAsync(session, null);
    }

    /// <summary>
    /// 根据报文类型分发处理。
    /// 使用协议处理器解析报文，实现版本无关的处理逻辑。
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="packetType">报文类型</param>
    /// <param name="flags">报文标志</param>
    /// <param name="payload">有效载荷</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task HandlePacketAsync(MqttClientSession session, MqttPacketType packetType, int flags, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        var handler = session.ProtocolHandler!;

        switch (packetType)
        {
            case MqttPacketType.Publish:
                var publishPacket = handler.PublishParser.Parse(payload.Span, (byte)flags);
                await HandlePublishAsync(session, publishPacket, cancellationToken);
                break;

            case MqttPacketType.PubAck:
            case MqttPacketType.PubRec:
            case MqttPacketType.PubRel:
            case MqttPacketType.PubComp:
                // QoS 确认报文，当前简单实现不做处理
                break;

            case MqttPacketType.Subscribe:
                var subscribePacket = handler.SubscribeParser.Parse(payload.Span, (byte)flags);
                await HandleSubscribeAsync(session, subscribePacket, cancellationToken);
                break;

            case MqttPacketType.Unsubscribe:
                var unsubscribePacket = handler.UnsubscribeParser.Parse(payload.Span, (byte)flags);
                await HandleUnsubscribeAsync(session, unsubscribePacket, cancellationToken);
                break;

            case MqttPacketType.PingReq:
                await HandlePingAsync(session, cancellationToken);
                break;

            case MqttPacketType.Disconnect:
                var disconnectPacket = handler.DisconnectParser.Parse(payload.Span, (byte)flags);
                await HandleDisconnectAsync(session, disconnectPacket, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// 处理 DISCONNECT 报文。
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="packet">DISCONNECT 报文</param>
    /// <param name="cancellationToken">取消令牌</param>
    private Task HandleDisconnectAsync(MqttClientSession session, MqttDisconnectPacket packet, CancellationToken cancellationToken)
    {
        // 客户端主动断开，清除遗嘱消息（MQTT 5.0 原因码 0x00 表示正常断开）
        if (packet.ReasonCode == 0)
        {
            session.WillMessage = null;
        }
        session.IsConnected = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 处理 PUBLISH 报文。
    /// 使用解析后的报文对象，无需手动解析。
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="packet">PUBLISH 报文</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task HandlePublishAsync(MqttClientSession session, MqttPublishPacket packet, CancellationToken cancellationToken)
    {
        var topic = packet.Topic;
        var qos = packet.QoS;
        var retain = packet.Retain;

        // 0. 处理 Topic Alias（MQTT 5.0）
        if (session.ProtocolVersion == MqttProtocolVersion.V500 && packet.Properties?.TopicAlias > 0)
        {
            var alias = packet.Properties.TopicAlias.Value;

            if (string.IsNullOrEmpty(topic))
            {
                // 只有 TopicAlias，从映射中查找 Topic
                if (!session.InboundTopicAliases.TryGetValue(alias, out var mappedTopic))
                {
                    // Topic Alias 无效
                    if (qos == MqttQualityOfService.AtLeastOnce)
                    {
                        var pubAckPacket = session.ProtocolHandler!.PubAckBuilder.CreatePubAck(packet.PacketId, 0x94); // Topic Alias 无效
                        await SendPacketAsync(session, pubAckPacket, cancellationToken);
                    }
                    else if (qos == MqttQualityOfService.ExactlyOnce)
                    {
                        var pubRecPacket = session.ProtocolHandler!.PubAckBuilder.CreatePubRec(packet.PacketId, 0x94);
                        await SendPacketAsync(session, pubRecPacket, cancellationToken);
                    }
                    return;
                }
                topic = mappedTopic;
            }
            else
            {
                // 有 Topic 和 TopicAlias，保存映射
                session.InboundTopicAliases[alias] = topic;
            }
        }

        // 1. 检查发布权限
        if (!await Authorizer.CanPublishAsync(session, topic, cancellationToken))
        {
            // 未授权时，QoS > 0 需要发送错误响应
            if (qos == MqttQualityOfService.AtLeastOnce)
            {
                var pubAckPacket = session.ProtocolHandler!.PubAckBuilder.CreatePubAck(packet.PacketId, 0x87); // 未授权
                await SendPacketAsync(session, pubAckPacket, cancellationToken);
            }
            else if (qos == MqttQualityOfService.ExactlyOnce)
            {
                var pubRecPacket = session.ProtocolHandler!.PubAckBuilder.CreatePubRec(packet.PacketId, 0x87); // 未授权
                await SendPacketAsync(session, pubRecPacket, cancellationToken);
            }
            return;
        }

        // 2. 检查消息大小
        if (packet.Payload.Length > Options.MaxMessageSize)
        {
            // 消息过大时，QoS > 0 需要发送错误响应
            if (qos == MqttQualityOfService.AtLeastOnce)
            {
                var pubAckPacket = session.ProtocolHandler!.PubAckBuilder.CreatePubAck(packet.PacketId, 0x95); // 报文过大
                await SendPacketAsync(session, pubAckPacket, cancellationToken);
            }
            else if (qos == MqttQualityOfService.ExactlyOnce)
            {
                var pubRecPacket = session.ProtocolHandler!.PubAckBuilder.CreatePubRec(packet.PacketId, 0x95); // 报文过大
                await SendPacketAsync(session, pubRecPacket, cancellationToken);
            }
            return;
        }

        // 3. 构建消息对象
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = packet.Payload,
            QualityOfService = qos,
            Retain = retain,
            SourceProtocol = MqttProtocolType.Mqtt,
            SourceClientId = session.ClientId,
            PublishedAt = DateTime.UtcNow
        };

        // 复制 MQTT 5.0 属性（如果有）
        if (packet.Properties != null)
        {
            message.PayloadFormatIndicator = packet.Properties.PayloadFormatIndicator;
            message.MessageExpiryInterval = packet.Properties.MessageExpiryInterval;
            message.TopicAlias = packet.Properties.TopicAlias;
            message.ResponseTopic = packet.Properties.ResponseTopic;
            message.CorrelationData = packet.Properties.CorrelationData;
            message.ContentType = packet.Properties.ContentType;
            foreach (var id in packet.Properties.SubscriptionIdentifiers)
            {
                message.SubscriptionIdentifiers.Add(id);
            }
            foreach (var prop in packet.Properties.UserProperties)
            {
                message.UserProperties.Add(prop);
            }
        }

        // 3. 触发 MessagePublishing 事件（允许用户控制是否处理）
        var publishingArgs = new MqttMessagePublishingEventArgs
        {
            Session = session,
            Protocol = MqttProtocolType.Mqtt,
            SourceClientId = session.ClientId,
            Message = message,
            ProcessMessage = true,
            SendAck = true,
            ReasonCode = 0x00
        };
        MessagePublishing?.Invoke(this, publishingArgs);

        // 4. 根据 QoS 发送确认（如果用户允许）
        if (publishingArgs.SendAck)
        {
            if (qos == MqttQualityOfService.AtLeastOnce)
            {
                var pubAckPacket = session.ProtocolHandler!.PubAckBuilder.CreatePubAck(packet.PacketId, publishingArgs.ReasonCode);
                await SendPacketAsync(session, pubAckPacket, cancellationToken);
            }
            else if (qos == MqttQualityOfService.ExactlyOnce)
            {
                var pubRecPacket = session.ProtocolHandler!.PubAckBuilder.CreatePubRec(packet.PacketId, publishingArgs.ReasonCode);
                await SendPacketAsync(session, pubRecPacket, cancellationToken);
            }
        }

        // 5. 如果用户不处理消息，直接返回
        if (!publishingArgs.ProcessMessage)
        {
            return;
        }

        // 6. 处理保留消息
        if (retain && Options.EnableRetainedMessages)
        {
            if (packet.Payload.IsEmpty)
            {
                _retainedMessages.TryRemove(topic, out _);
            }
            else
            {
                _retainedMessages[topic] = message;
            }
        }

        // 7. 分发消息到订阅者
        var deliveredCount = await DistributeMessageAsync(message, session.ClientId, cancellationToken, session, MqttProtocolType.Mqtt);

        // 8. 触发事件（异步通知）
        if (deliveredCount == 0)
        {
            // 无接收者事件
            _eventDispatcher.Dispatch(BrokerEventType.MessageNotDelivered,
                new MqttMessageNotDeliveredEventArgs
                {
                    Session = session,
                    Protocol = MqttProtocolType.Mqtt,
                    SourceClientId = session.ClientId,
                    Message = message
                }, MessageNotDelivered);
        }

        // 触发 MessagePublished 完成事件
        _eventDispatcher.Dispatch(BrokerEventType.MessagePublished,
            new MqttMessagePublishedEventArgs
            {
                Session = session,
                Protocol = MqttProtocolType.Mqtt,
                SourceClientId = session.ClientId,
                Message = message,
                DeliveredCount = deliveredCount
            }, MessagePublished);
    }

    /// <summary>
    /// 发送报文（使用协议处理器序列化）。
    /// </summary>
    private async Task SendPacketAsync(MqttClientSession session, IMqttPacket packet, CancellationToken cancellationToken)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        session.ProtocolHandler!.WritePacket(packet, bufferWriter);
        await SendPacketAsync(session, bufferWriter.WrittenMemory, cancellationToken);
    }

    /// <summary>
    /// 处理 SUBSCRIBE 报文。
    /// 流程：触发事件 → 检查权限 → 发送 SUBACK → 发送保留消息 → 触发完成事件
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="packet">SUBSCRIBE 报文</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task HandleSubscribeAsync(MqttClientSession session, MqttSubscribePacket packet, CancellationToken cancellationToken)
    {
        // 1. 构建订阅请求列表
        var requests = new List<MqttSubscriptionRequest>(packet.Subscriptions.Count);
        foreach (var sub in packet.Subscriptions)
        {
            requests.Add(new MqttSubscriptionRequest
            {
                TopicFilter = sub.TopicFilter,
                RequestedQoS = sub.QoS,
                GrantedQoS = sub.QoS,
                Accept = true
            });
        }

        // 2. 触发 ClientSubscribing 事件（允许用户修改每个请求）
        var subscribingArgs = new MqttClientSubscribingEventArgs { Session = session, Subscriptions = requests };
        ClientSubscribing?.Invoke(this, subscribingArgs);

        // 3. 处理每个订阅请求，检查权限和用户设置
        var successfulSubscriptions = new List<MqttTopicSubscription>();
        var reasonCodes = new List<byte>(requests.Count);
        var topicsToSendRetained = new List<string>();

        foreach (var request in requests)
        {
            // 用户拒绝
            if (!request.Accept)
            {
                reasonCodes.Add(request.RejectReasonCode);
                continue;
            }

            // 检查授权
            if (!await Authorizer.CanSubscribeAsync(session, request.TopicFilter, cancellationToken))
            {
                reasonCodes.Add(0x87); // 未授权
                continue;
            }

            // 订阅成功
            session.Subscriptions.Add(request.TopicFilter);
            session.SubscriptionQos[request.TopicFilter] = request.GrantedQoS;
            successfulSubscriptions.Add(new MqttTopicSubscription
            {
                Topic = request.TopicFilter,
                QualityOfService = request.GrantedQoS
            });
            reasonCodes.Add((byte)request.GrantedQoS);
            topicsToSendRetained.Add(request.TopicFilter);
        }

        // 4. 发送 SUBACK 响应
        var subAckPacket = session.ProtocolHandler!.SubAckBuilder.Create(packet.PacketId, reasonCodes);
        await SendPacketAsync(session, subAckPacket, cancellationToken);

        // 5. 发送匹配的保留消息（仅对成功订阅的主题）
        foreach (var topic in topicsToSendRetained)
        {
            foreach (var retained in _retainedMessages.Values)
            {
                if (TopicMatches(topic, retained.Topic))
                {
                    await SendPublishAsync(session, retained, cancellationToken);

                    // 触发消息已投递事件（使用保留消息自身的来源信息）
                    _eventDispatcher.Dispatch(BrokerEventType.MessageDelivered,
                        new MqttMessageDeliveredEventArgs
                        {
                            SourceSession = null,
                            Protocol = retained.SourceProtocol ?? MqttProtocolType.Internal,
                            SourceClientId = retained.SourceClientId,
                            TargetSession = session,
                            Message = retained
                        }, MessageDelivered);
                }
            }
        }

        // 6. 触发 ClientSubscribed 完成事件（异步通知）
        if (successfulSubscriptions.Count > 0)
        {
            _eventDispatcher.Dispatch(BrokerEventType.ClientSubscribed,
                new MqttClientSubscribedEventArgs
                {
                    Session = session,
                    Subscriptions = successfulSubscriptions
                }, ClientSubscribed);
        }
    }

    /// <summary>
    /// 处理 UNSUBSCRIBE 报文。
    /// 流程：触发事件 → 检查用户设置 → 执行取消订阅 → 发送 UNSUBACK → 触发完成事件
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="packet">UNSUBSCRIBE 报文</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task HandleUnsubscribeAsync(MqttClientSession session, MqttUnsubscribePacket packet, CancellationToken cancellationToken)
    {
        // 1. 构建取消订阅请求列表
        var requests = new List<MqttUnsubscribeRequest>(packet.TopicFilters.Count);
        foreach (var topic in packet.TopicFilters)
        {
            requests.Add(new MqttUnsubscribeRequest
            {
                TopicFilter = topic,
                Accept = true
            });
        }

        // 2. 触发 ClientUnsubscribing 事件（允许用户修改每个请求）
        var unsubscribingArgs = new MqttClientUnsubscribingEventArgs { Session = session, Unsubscriptions = requests };
        ClientUnsubscribing?.Invoke(this, unsubscribingArgs);

        // 3. 处理每个取消订阅请求
        var successfulTopics = new List<string>();
        var reasonCodes = new List<byte>(requests.Count);

        foreach (var request in requests)
        {
            if (!request.Accept)
            {
                // 用户拒绝取消订阅
                reasonCodes.Add(request.RejectReasonCode);
                continue;
            }

            // 执行取消订阅
            session.Subscriptions.Remove(request.TopicFilter);
            session.SubscriptionQos.Remove(request.TopicFilter);
            successfulTopics.Add(request.TopicFilter);
            reasonCodes.Add(0x00); // 成功
        }

        // 4. 发送 UNSUBACK 响应
        // MQTT 3.1.1 不需要原因码，V311UnsubAckBuilder.Create 会忽略 reasonCodes
        var unsubAckPacket = session.ProtocolHandler!.UnsubAckBuilder.Create(packet.PacketId, reasonCodes);
        await SendPacketAsync(session, unsubAckPacket, cancellationToken);

        // 5. 触发 ClientUnsubscribed 完成事件（异步通知）
        if (successfulTopics.Count > 0)
        {
            _eventDispatcher.Dispatch(BrokerEventType.ClientUnsubscribed,
                new MqttClientUnsubscribedEventArgs
                {
                    Session = session,
                    Topics = successfulTopics
                }, ClientUnsubscribed);
        }
    }

    /// <summary>
    /// 处理 PINGREQ 报文，发送 PINGRESP 响应。
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task HandlePingAsync(MqttClientSession session, CancellationToken cancellationToken)
    {
        // 使用协议处理器获取 PINGRESP 报文
        var pingResp = session.ProtocolHandler!.PingHandler.GetPingRespBytes();
        await SendPacketAsync(session, pingResp, cancellationToken);
    }

    /// <summary>
    /// 处理客户端断开连接。
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="exception">导致断开的异常（如果有）</param>
    private async Task HandleClientDisconnectAsync(MqttClientSession session, Exception? exception)
    {
        _sessions.TryRemove(session.ClientId, out _);

        // 保存持久化会话
        if (!session.CleanSession && Options.EnablePersistentSessions)
        {
            _persistentSessions[session.ClientId] = session;
        }

        // 发送遗嘱消息
        if (session.WillMessage != null)
        {
            await DistributeMessageAsync(session.WillMessage, session.ClientId, CancellationToken.None, session, MqttProtocolType.Mqtt);
        }

        // 清理资源
        session.IsConnected = false;
        if (session.Transport != null)
        {
            await session.Transport.DisposeAsync();
        }
        session.SendLock.Dispose();

        // 触发断开连接事件（异步通知）
        _eventDispatcher.Dispatch(BrokerEventType.ClientDisconnected,
            new MqttClientDisconnectedEventArgs
            {
                Session = session,
                Exception = exception,
                Graceful = exception == null
            }, ClientDisconnected);
    }

    /// <summary>
    /// 将消息分发到所有匹配的订阅者。
    /// </summary>
    /// <param name="message">要分发的消息</param>
    /// <param name="sourceClientId">消息来源客户端 ID（可选，用于避免回环）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="sourceSession">消息来源会话（可选，用于事件）</param>
    /// <param name="protocol">消息来源协议类型</param>
    /// <returns>投递成功的客户端数量</returns>
    private async Task<int> DistributeMessageAsync(
        MqttApplicationMessage message,
        string? sourceClientId,
        CancellationToken cancellationToken,
        MqttClientSession? sourceSession = null,
        MqttProtocolType protocol = MqttProtocolType.Mqtt)
    {
        var deliveredCount = 0;

        foreach (var session in _sessions.Values)
        {
            // 跳过消息来源
            if (session.ClientId == sourceClientId) continue;

            foreach (var subscription in session.Subscriptions)
            {
                if (TopicMatches(subscription, message.Topic))
                {
                    // 确定投递的 QoS（取订阅 QoS 和消息 QoS 的较小值）
                    var qos = session.SubscriptionQos.TryGetValue(subscription, out var subQos)
                        ? (MqttQualityOfService)Math.Min((int)subQos, (int)message.QualityOfService)
                        : MqttQualityOfService.AtMostOnce;

                    var deliveryMessage = new MqttApplicationMessage
                    {
                        Topic = message.Topic,
                        Payload = message.Payload,
                        QualityOfService = qos,
                        Retain = false // 投递时不设置 Retain
                    };

                    await SendPublishAsync(session, deliveryMessage, cancellationToken);
                    deliveredCount++;

                    // 触发消息已投递事件（异步通知）
                    _eventDispatcher.Dispatch(BrokerEventType.MessageDelivered,
                        new MqttMessageDeliveredEventArgs
                        {
                            SourceSession = sourceSession,
                            Protocol = protocol,
                            SourceClientId = sourceClientId,
                            TargetSession = session,
                            Message = deliveryMessage
                        }, MessageDelivered);

                    break; // 每个客户端只投递一次
                }
            }
        }

        return deliveredCount;
    }

    /// <summary>
    /// 发送 PUBLISH 报文到客户端。
    /// 使用协议处理器构建报文，确保正确的协议版本格式。
    /// </summary>
    /// <param name="session">目标客户端会话</param>
    /// <param name="message">要发送的消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task SendPublishAsync(MqttClientSession session, MqttApplicationMessage message, CancellationToken cancellationToken)
    {
        // 获取报文标识符（QoS > 0 时需要）
        var packetId = message.QualityOfService != MqttQualityOfService.AtMostOnce
            ? session.GetNextPacketId()
            : (ushort)0;

        // 使用协议处理器构建 PUBLISH 报文
        var packet = session.ProtocolHandler!.PublishBuilder.CreateFromMessage(message, packetId);

        // 发送报文
        await SendPacketAsync(session, packet, cancellationToken);
    }

    /// <summary>
    /// 发送报文到客户端。
    /// 使用发送锁确保线程安全。
    /// </summary>
    /// <param name="session">目标客户端会话</param>
    /// <param name="packet">报文数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SendPacketAsync(MqttClientSession session, byte[] packet, CancellationToken cancellationToken)
    {
        await session.SendLock.WaitAsync(cancellationToken);
        try
        {
            if (session.Transport != null && session.IsConnected)
            {
                await session.Transport.WriteAsync(packet.AsMemory(), cancellationToken);
                await session.Transport.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            session.SendLock.Release();
        }
    }

    /// <summary>
    /// 发送报文到客户端（Memory 重载）。
    /// </summary>
    /// <param name="session">目标客户端会话</param>
    /// <param name="packet">报文数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SendPacketAsync(MqttClientSession session, ReadOnlyMemory<byte> packet, CancellationToken cancellationToken)
    {
        await session.SendLock.WaitAsync(cancellationToken);
        try
        {
            if (session.Transport != null && session.IsConnected)
            {
                await session.Transport.WriteAsync(packet, cancellationToken);
                await session.Transport.FlushAsync(cancellationToken);
            }
        }
        finally
        {
            session.SendLock.Release();
        }
    }

    /// <summary>
    /// 检查对象是否已释放。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttBroker));
        }
    }

    /// <summary>
    /// 从传输连接中精确读取指定字节数。
    /// </summary>
    /// <param name="connection">传输连接</param>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="offset">缓冲区偏移量</param>
    /// <param name="count">要读取的字节数</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static async Task ReadExactlyAsync(ITransportConnection connection, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await connection.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), cancellationToken);
            if (read == 0)
            {
                throw new EndOfStreamException("连接意外关闭");
            }
            totalRead += read;
        }
    }

    /// <summary>
    /// 检查主题是否匹配订阅过滤器。
    /// 支持 + 和 # 通配符。
    /// </summary>
    /// <param name="filter">订阅过滤器</param>
    /// <param name="topic">主题名称</param>
    /// <returns>是否匹配</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool TopicMatches(string filter, string topic)
    {
        // 完全匹配
        if (filter == topic) return true;

        // # 匹配所有
        if (filter == "#") return true;

        var filterParts = filter.Split('/');
        var topicParts = topic.Split('/');

        for (int i = 0; i < filterParts.Length; i++)
        {
            // # 匹配剩余所有层级
            if (filterParts[i] == "#")
            {
                return true;
            }

            // 主题层级不足
            if (i >= topicParts.Length)
            {
                return false;
            }

            // + 匹配单个层级，其他需要精确匹配
            if (filterParts[i] != "+" && filterParts[i] != topicParts[i])
            {
                return false;
            }
        }

        // 层级数必须相等
        return filterParts.Length == topicParts.Length;
    }

    /// <summary>
    /// 从传输连接中异步解码 MQTT 剩余长度字段。
    /// 使用 ArrayPool 减少内存分配。
    /// </summary>
    /// <param name="connection">传输连接</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解码后的长度值</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static async Task<int> DecodeRemainingLengthAsync(ITransportConnection connection, CancellationToken cancellationToken)
    {
        var multiplier = 1;
        var value = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            byte encodedByte;
            do
            {
                var read = await connection.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
                if (read == 0)
                {
                    throw new EndOfStreamException("连接意外关闭");
                }
                encodedByte = buffer[0];
                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;

                // 防止恶意报文
                if (multiplier > 128 * 128 * 128 * 128)
                {
                    throw new InvalidOperationException("剩余长度字段格式错误");
                }
            } while ((encodedByte & 128) != 0);

            return value;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();

        // 停止所有传输监听器
        foreach (var listener in _transportListeners)
        {
            listener.Stop();
        }
        _transportListeners.Clear();

        // 清理所有会话
        foreach (var session in _sessions.Values)
        {
            try
            {
                session.Transport?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // 忽略释放时的异常
            }
            session.SendLock.Dispose();
        }

        _sessions.Clear();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await StopAsync();

        // 释放所有桥接
        foreach (var bridge in _bridges)
        {
            try
            {
                await bridge.DisposeAsync();
            }
            catch
            {
                // 忽略释放桥接时的错误
            }
        }
        _bridges.Clear();

        // 释放集群节点
        if (_cluster != null)
        {
            try
            {
                await _cluster.DisposeAsync();
            }
            catch
            {
                // 忽略释放集群时的错误
            }
            _cluster = null;
        }

        await _eventDispatcher.DisposeAsync();
        Dispose();
    }
}
