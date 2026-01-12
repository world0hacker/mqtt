using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// MQTT 集群管理器。
/// 管理多个 Broker 节点之间的通信和消息路由。
/// </summary>
public sealed class MqttCluster : IMqttClusterNode
{
    private readonly MqttBroker _broker;
    private readonly MqttClusterOptions _options;
    private readonly ConcurrentDictionary<string, ClusterPeer> _peers = new();
    private readonly ConcurrentDictionary<string, DateTime> _messageIdCache = new(); // 消息去重
    private readonly ConcurrentDictionary<string, HashSet<string>> _clusterSubscriptions = new(); // 集群订阅表

    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private Task? _heartbeatTask;
    private Task? _cleanupTask;
    private bool _disposed;

    /// <summary>
    /// 获取节点 ID。
    /// </summary>
    public string NodeId => _options.NodeId;

    /// <summary>
    /// 获取节点是否正在运行。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 获取已发现的对等节点。
    /// </summary>
    public IReadOnlyCollection<ClusterPeerInfo> Peers => _peers.Values
        .Select(p => p.Info)
        .ToList()
        .AsReadOnly();

    /// <summary>
    /// 获取集群配置。
    /// </summary>
    public MqttClusterOptions Options => _options;

    /// <summary>
    /// 当对等节点加入时触发。
    /// </summary>
    public event EventHandler<ClusterPeerEventArgs>? PeerJoined;

    /// <summary>
    /// 当对等节点离开时触发。
    /// </summary>
    public event EventHandler<ClusterPeerEventArgs>? PeerLeft;

    /// <summary>
    /// 当消息被转发时触发。
    /// </summary>
    public event EventHandler<ClusterMessageForwardedEventArgs>? MessageForwarded;

    /// <summary>
    /// 当订阅信息同步时触发。
    /// </summary>
    public event EventHandler<ClusterSubscriptionSyncEventArgs>? SubscriptionSynced;

    /// <summary>
    /// 当保留消息同步时触发。
    /// </summary>
    public event EventHandler<ClusterRetainedSyncEventArgs>? RetainedMessagesSynced;

    /// <summary>
    /// 创建集群管理器。
    /// </summary>
    /// <param name="broker">本地 Broker</param>
    /// <param name="options">集群配置</param>
    public MqttCluster(MqttBroker broker, MqttClusterOptions options)
    {
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 启动集群节点。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (IsRunning) return;

        _cts = new CancellationTokenSource();

        // 启动集群监听
        var endpoint = IPAddress.Parse(_options.BindAddress);
        _listener = new TcpListener(endpoint, _options.ClusterPort);
        _listener.Start();
        _acceptTask = AcceptPeersAsync(_cts.Token);

        // 启动心跳任务
        _heartbeatTask = HeartbeatLoopAsync(_cts.Token);

        // 启动清理任务（清理过期的消息 ID）
        _cleanupTask = CleanupLoopAsync(_cts.Token);

        // 注册 Broker 事件
        _broker.MessagePublished += OnLocalMessagePublished;
        _broker.ClientSubscribed += OnLocalClientSubscribed;
        _broker.ClientUnsubscribed += OnLocalClientUnsubscribed;

        // 连接到种子节点
        foreach (var seedNode in _options.SeedNodes)
        {
            _ = ConnectToPeerAsync(seedNode, _cts.Token);
        }

        IsRunning = true;
    }

    /// <summary>
    /// 停止集群节点。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) return;
        IsRunning = false;

        // 取消订阅事件
        _broker.MessagePublished -= OnLocalMessagePublished;
        _broker.ClientSubscribed -= OnLocalClientSubscribed;
        _broker.ClientUnsubscribed -= OnLocalClientUnsubscribed;

        _cts?.Cancel();
        _listener?.Stop();

        // 通知其他节点本节点离开
        var leaveMessage = new ClusterMessage
        {
            Type = ClusterMessageType.NodeLeave,
            SourceNodeId = NodeId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        foreach (var peer in _peers.Values.Where(p => p.IsConnected))
        {
            try
            {
                await peer.SendAsync(leaveMessage, cancellationToken);
            }
            catch
            {
                // 忽略发送失败
            }
        }

        // 断开所有对等节点
        foreach (var peer in _peers.Values)
        {
            peer.Dispose();
        }
        _peers.Clear();

        // 等待任务完成
        try
        {
            if (_acceptTask != null) await _acceptTask;
            if (_heartbeatTask != null) await _heartbeatTask;
            if (_cleanupTask != null) await _cleanupTask;
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
    }

    /// <summary>
    /// 广播消息到集群（避免循环转发）。
    /// </summary>
    /// <param name="message">MQTT 应用消息</param>
    /// <param name="sourceNodeId">消息来源节点 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task BroadcastMessageAsync(MqttApplicationMessage message, string sourceNodeId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // 生成消息唯一 ID（用于去重）
        var messageId = GenerateMessageId(message, sourceNodeId);

        // 检查是否已处理过（去重）
        if (_options.EnableDeduplication)
        {
            if (_messageIdCache.ContainsKey(messageId))
            {
                return; // 消息已处理，跳过
            }
            _messageIdCache[messageId] = DateTime.UtcNow;
        }

        // 构建集群消息
        var clusterMessage = ClusterMessage.CreatePublish(sourceNodeId, message, messageId);

        // 发送给所有对等节点（除了来源节点）
        var forwardedCount = 0;
        var tasks = new List<Task>();

        foreach (var peer in _peers.Values.Where(p => p.Info.NodeId != sourceNodeId && p.IsConnected))
        {
            tasks.Add(SendToPeerAsync(peer, clusterMessage, cancellationToken));
            forwardedCount++;
        }

        await Task.WhenAll(tasks);

        MessageForwarded?.Invoke(this, new ClusterMessageForwardedEventArgs
        {
            SourceNodeId = sourceNodeId,
            Topic = message.Topic,
            PayloadSize = message.Payload.Length,
            ForwardedToCount = forwardedCount
        });
    }

    /// <summary>
    /// 同步订阅信息到集群。
    /// </summary>
    /// <param name="topic">主题过滤器</param>
    /// <param name="isSubscribe">是否为订阅操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SyncSubscriptionAsync(string topic, bool isSubscribe, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var clusterMessage = isSubscribe
            ? ClusterMessage.CreateSubscribe(NodeId, topic)
            : ClusterMessage.CreateUnsubscribe(NodeId, topic);

        var tasks = _peers.Values
            .Where(p => p.IsConnected)
            .Select(p => SendToPeerAsync(p, clusterMessage, cancellationToken));

        await Task.WhenAll(tasks);

        SubscriptionSynced?.Invoke(this, new ClusterSubscriptionSyncEventArgs
        {
            SourceNodeId = NodeId,
            TopicFilter = topic,
            IsSubscribe = isSubscribe
        });
    }

    /// <summary>
    /// 本地消息发布时，广播到集群。
    /// </summary>
    private async void OnLocalMessagePublished(object? sender, MqttMessagePublishedEventArgs e)
    {
        if (!IsRunning) return;

        try
        {
            await BroadcastMessageAsync(e.Message, NodeId, CancellationToken.None);
        }
        catch
        {
            // 忽略广播失败
        }
    }

    /// <summary>
    /// 本地客户端订阅时，同步到集群。
    /// </summary>
    private async void OnLocalClientSubscribed(object? sender, MqttClientSubscribedEventArgs e)
    {
        if (!IsRunning) return;

        foreach (var sub in e.Subscriptions)
        {
            try
            {
                // 更新本地集群订阅表
                if (!_clusterSubscriptions.ContainsKey(sub.Topic))
                {
                    _clusterSubscriptions[sub.Topic] = new HashSet<string>();
                }
                _clusterSubscriptions[sub.Topic].Add(NodeId);

                // 同步到集群
                await SyncSubscriptionAsync(sub.Topic, true, CancellationToken.None);
            }
            catch
            {
                // 忽略同步失败
            }
        }
    }

    /// <summary>
    /// 本地客户端取消订阅时，同步到集群。
    /// </summary>
    private async void OnLocalClientUnsubscribed(object? sender, MqttClientUnsubscribedEventArgs e)
    {
        if (!IsRunning) return;

        foreach (var topic in e.Topics)
        {
            try
            {
                // 检查是否还有其他本地订阅者
                var hasLocalSubscribers = _broker.Sessions
                    .Any(s => s.Subscriptions.Any(sub => TopicMatches(sub, topic)));

                if (!hasLocalSubscribers)
                {
                    if (_clusterSubscriptions.TryGetValue(topic, out var nodes))
                    {
                        nodes.Remove(NodeId);
                    }
                    await SyncSubscriptionAsync(topic, false, CancellationToken.None);
                }
            }
            catch
            {
                // 忽略同步失败
            }
        }
    }

    /// <summary>
    /// 处理从对等节点收到的消息。
    /// </summary>
    private async Task HandleClusterMessageAsync(ClusterMessage message, ClusterPeer peer)
    {
        switch (message.Type)
        {
            case ClusterMessageType.Publish:
                await HandleClusterPublishAsync(message);
                break;

            case ClusterMessageType.Subscribe:
                HandleClusterSubscribe(message);
                break;

            case ClusterMessageType.Unsubscribe:
                HandleClusterUnsubscribe(message);
                break;

            case ClusterMessageType.Heartbeat:
                peer.LastHeartbeat = DateTime.UtcNow;
                peer.Info.LastHeartbeat = DateTime.UtcNow;
                break;

            case ClusterMessageType.NodeLeave:
                RemovePeer(peer, "节点主动离开");
                break;

            case ClusterMessageType.RetainedSyncRequest:
                await HandleRetainedSyncRequestAsync(peer);
                break;

            case ClusterMessageType.RetainedSyncData:
                HandleRetainedSyncData(message);
                break;
        }
    }

    /// <summary>
    /// 处理保留消息同步请求（发送本地保留消息给请求方）。
    /// </summary>
    private async Task HandleRetainedSyncRequestAsync(ClusterPeer peer)
    {
        var retainedMessages = _broker.RetainedMessages.Values.ToList();
        var syncData = ClusterMessage.CreateRetainedSyncData(NodeId, retainedMessages);
        await SendToPeerAsync(peer, syncData, CancellationToken.None);

        RetainedMessagesSynced?.Invoke(this, new ClusterRetainedSyncEventArgs
        {
            SourceNodeId = peer.Info.NodeId,
            MessageCount = retainedMessages.Count,
            IsRequest = true
        });
    }

    /// <summary>
    /// 处理保留消息同步数据（存储接收到的保留消息）。
    /// </summary>
    private void HandleRetainedSyncData(ClusterMessage message)
    {
        var messages = message.ParseRetainedMessages();
        _broker.SetRetainedMessages(messages);

        RetainedMessagesSynced?.Invoke(this, new ClusterRetainedSyncEventArgs
        {
            SourceNodeId = message.SourceNodeId,
            MessageCount = messages.Count,
            IsRequest = false
        });
    }

    /// <summary>
    /// 请求对等节点的保留消息。
    /// </summary>
    private async Task RequestRetainedMessagesAsync(ClusterPeer peer, CancellationToken cancellationToken)
    {
        var request = ClusterMessage.CreateRetainedSyncRequest(NodeId);
        await SendToPeerAsync(peer, request, cancellationToken);
    }

    /// <summary>
    /// 处理集群发布消息（转发到本地订阅者）。
    /// </summary>
    private async Task HandleClusterPublishAsync(ClusterMessage clusterMessage)
    {
        // 检查消息去重
        if (_options.EnableDeduplication)
        {
            if (_messageIdCache.ContainsKey(clusterMessage.MessageId))
            {
                return;
            }
            _messageIdCache[clusterMessage.MessageId] = DateTime.UtcNow;
        }

        // 构建 MQTT 消息
        var message = new MqttApplicationMessage
        {
            Topic = clusterMessage.Topic ?? string.Empty,
            Payload = clusterMessage.Payload,
            QualityOfService = clusterMessage.QoS,
            Retain = clusterMessage.Retain
        };

        // 发布到本地 Broker（只分发给本地订阅者）
        await _broker.PublishAsync(message);

        // 继续转发给其他节点
        await BroadcastMessageAsync(message, clusterMessage.SourceNodeId, CancellationToken.None);
    }

    /// <summary>
    /// 处理集群订阅同步。
    /// </summary>
    private void HandleClusterSubscribe(ClusterMessage message)
    {
        if (string.IsNullOrEmpty(message.Topic)) return;

        if (!_clusterSubscriptions.ContainsKey(message.Topic))
        {
            _clusterSubscriptions[message.Topic] = new HashSet<string>();
        }
        _clusterSubscriptions[message.Topic].Add(message.SourceNodeId);

        SubscriptionSynced?.Invoke(this, new ClusterSubscriptionSyncEventArgs
        {
            SourceNodeId = message.SourceNodeId,
            TopicFilter = message.Topic,
            IsSubscribe = true
        });
    }

    /// <summary>
    /// 处理集群取消订阅同步。
    /// </summary>
    private void HandleClusterUnsubscribe(ClusterMessage message)
    {
        if (string.IsNullOrEmpty(message.Topic)) return;

        if (_clusterSubscriptions.TryGetValue(message.Topic, out var nodes))
        {
            nodes.Remove(message.SourceNodeId);
        }

        SubscriptionSynced?.Invoke(this, new ClusterSubscriptionSyncEventArgs
        {
            SourceNodeId = message.SourceNodeId,
            TopicFilter = message.Topic,
            IsSubscribe = false
        });
    }

    /// <summary>
    /// 接受对等节点连接。
    /// </summary>
    private async Task AcceptPeersAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener!.AcceptTcpClientAsync(cancellationToken);
                _ = HandleInboundConnectionAsync(tcpClient, cancellationToken);
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
    /// 处理入站连接。
    /// </summary>
    private async Task HandleInboundConnectionAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        ClusterPeer? peer = null;

        try
        {
            // 临时创建 peer 用于握手
            var tempInfo = new ClusterPeerInfo
            {
                NodeId = "unknown",
                Address = tcpClient.Client.RemoteEndPoint?.ToString() ?? "",
                IsInbound = true
            };
            peer = new ClusterPeer(tcpClient, tempInfo);

            // 接收握手请求
            var remoteHandshake = await peer.ReceiveHandshakeAsync(cancellationToken);
            if (remoteHandshake == null)
            {
                peer.Dispose();
                return;
            }

            // 验证集群名称
            if (remoteHandshake.ClusterName != _options.ClusterName)
            {
                peer.Dispose();
                return;
            }

            // 避免自连接
            if (remoteHandshake.NodeId == NodeId)
            {
                peer.Dispose();
                return;
            }

            // 发送握手响应
            var localHandshake = ClusterHandshake.Create(NodeId, _options.ClusterName, _options.ClusterPort);
            await peer.SendHandshakeAsync(localHandshake, true, cancellationToken);

            // 检查是否已存在连接
            if (_peers.ContainsKey(remoteHandshake.NodeId))
            {
                peer.Dispose();
                return;
            }

            // 创建正式的 peer（重用 TcpClient，不要 Dispose 临时 peer）
            var peerInfo = new ClusterPeerInfo
            {
                NodeId = remoteHandshake.NodeId,
                Address = tcpClient.Client.RemoteEndPoint?.ToString() ?? "",
                ListenPort = remoteHandshake.ListenPort,
                IsInbound = true
            };

            // 用新信息重新创建 peer（临时 peer 不再使用，但不能 Dispose 因为共享 TcpClient）
            peer = new ClusterPeer(tcpClient, peerInfo);

            _peers[remoteHandshake.NodeId] = peer;
            PeerJoined?.Invoke(this, new ClusterPeerEventArgs { Peer = peerInfo });

            // 请求保留消息同步
            _ = RequestRetainedMessagesAsync(peer, cancellationToken);

            // 开始接收消息
            await ReceiveFromPeerAsync(peer, cancellationToken);
        }
        catch
        {
            peer?.Dispose();
        }
    }

    /// <summary>
    /// 连接到对等节点。
    /// </summary>
    private async Task ConnectToPeerAsync(string address, CancellationToken cancellationToken)
    {
        try
        {
            var parts = address.Split(':');
            var host = parts[0];
            var port = parts.Length > 1 ? int.Parse(parts[1]) : _options.ClusterPort;

            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, cancellationToken);

            // 发送握手请求
            var tempInfo = new ClusterPeerInfo
            {
                NodeId = "unknown",
                Address = address,
                IsInbound = false
            };
            var peer = new ClusterPeer(tcpClient, tempInfo);

            var localHandshake = ClusterHandshake.Create(NodeId, _options.ClusterName, _options.ClusterPort);
            await peer.SendHandshakeAsync(localHandshake, false, cancellationToken);

            // 接收握手响应
            var remoteHandshake = await peer.ReceiveHandshakeAsync(cancellationToken);
            if (remoteHandshake == null)
            {
                peer.Dispose();
                return;
            }

            // 验证集群名称
            if (remoteHandshake.ClusterName != _options.ClusterName)
            {
                peer.Dispose();
                return;
            }

            // 避免自连接
            if (remoteHandshake.NodeId == NodeId)
            {
                peer.Dispose();
                return;
            }

            // 检查是否已存在连接
            if (_peers.ContainsKey(remoteHandshake.NodeId))
            {
                peer.Dispose();
                return;
            }

            // 创建正式的 peer（重用 TcpClient，不要 Dispose 临时 peer）
            var peerInfo = new ClusterPeerInfo
            {
                NodeId = remoteHandshake.NodeId,
                Address = address,
                ListenPort = remoteHandshake.ListenPort,
                IsInbound = false
            };

            // 用新信息重新创建 peer
            peer = new ClusterPeer(tcpClient, peerInfo);

            _peers[remoteHandshake.NodeId] = peer;
            PeerJoined?.Invoke(this, new ClusterPeerEventArgs { Peer = peerInfo });

            // 请求保留消息同步
            _ = RequestRetainedMessagesAsync(peer, cancellationToken);

            // 开始接收消息
            _ = ReceiveFromPeerAsync(peer, cancellationToken);
        }
        catch
        {
            // 连接失败，稍后会通过心跳机制重试
        }
    }

    /// <summary>
    /// 从对等节点接收消息。
    /// </summary>
    private async Task ReceiveFromPeerAsync(ClusterPeer peer, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && peer.IsConnected)
            {
                var message = await peer.ReceiveAsync(cancellationToken);
                if (message == null)
                {
                    break; // 连接关闭
                }

                await HandleClusterMessageAsync(message, peer);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        catch
        {
            // 连接错误
        }
        finally
        {
            RemovePeer(peer, "连接断开");
        }
    }

    /// <summary>
    /// 移除对等节点。
    /// </summary>
    private void RemovePeer(ClusterPeer peer, string reason)
    {
        if (_peers.TryRemove(peer.Info.NodeId, out _))
        {
            PeerLeft?.Invoke(this, new ClusterPeerEventArgs { Peer = peer.Info });
            peer.Dispose();
        }
    }

    /// <summary>
    /// 发送消息到对等节点。
    /// </summary>
    private async Task SendToPeerAsync(ClusterPeer peer, ClusterMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await peer.SendAsync(message, cancellationToken);
        }
        catch
        {
            // 发送失败，可能需要重连
        }
    }

    /// <summary>
    /// 心跳循环。
    /// </summary>
    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.HeartbeatIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var heartbeat = ClusterMessage.CreateHeartbeat(NodeId);

            // 发送心跳
            foreach (var peer in _peers.Values.Where(p => p.IsConnected).ToList())
            {
                try
                {
                    await peer.SendAsync(heartbeat, cancellationToken);
                }
                catch
                {
                    // 忽略发送失败
                }
            }

            // 检查超时节点
            var timeout = TimeSpan.FromMilliseconds(_options.NodeTimeoutMs);
            foreach (var peer in _peers.Values.ToList())
            {
                if (peer.Info.TimeSinceLastHeartbeat > timeout)
                {
                    RemovePeer(peer, "心跳超时");
                }
            }
        }
    }

    /// <summary>
    /// 清理过期消息 ID 的循环。
    /// </summary>
    private async Task CleanupLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.MessageIdCacheExpirySeconds / 2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var expiry = DateTime.UtcNow.AddSeconds(-_options.MessageIdCacheExpirySeconds);
            foreach (var kvp in _messageIdCache)
            {
                if (kvp.Value < expiry)
                {
                    _messageIdCache.TryRemove(kvp.Key, out _);
                }
            }
        }
    }

    /// <summary>
    /// 生成消息唯一 ID（用于去重）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static string GenerateMessageId(MqttApplicationMessage message, string sourceNodeId)
    {
        // 基于 Topic、Payload 哈希和源节点生成唯一 ID
        var hash = ComputeHash(message.Payload.Span);
        return $"{sourceNodeId}:{message.Topic}:{hash}:{DateTime.UtcNow.Ticks}";
    }

    /// <summary>
    /// 计算载荷哈希。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ComputeHash(ReadOnlySpan<byte> data)
    {
        unchecked
        {
            int hash = 17;
            foreach (var b in data)
            {
                hash = hash * 31 + b;
            }
            return hash.ToString("X8");
        }
    }

    /// <summary>
    /// 主题匹配（支持 + 和 # 通配符）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool TopicMatches(string filter, string topic)
    {
        if (filter == topic) return true;
        if (filter == "#") return true;

        var filterParts = filter.Split('/');
        var topicParts = topic.Split('/');

        for (int i = 0; i < filterParts.Length; i++)
        {
            if (filterParts[i] == "#") return true;
            if (i >= topicParts.Length) return false;
            if (filterParts[i] != "+" && filterParts[i] != topicParts[i]) return false;
        }

        return filterParts.Length == topicParts.Length;
    }

    /// <summary>
    /// 检查对象是否已释放。
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttCluster));
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await StopAsync();
        _cts?.Dispose();
    }
}
