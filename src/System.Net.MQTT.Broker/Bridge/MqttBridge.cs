using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Broker.Bridge;

/// <summary>
/// MQTT 桥接实现。
/// 使用现有 MqttClient 连接到远程 Broker，实现消息双向同步。
/// </summary>
public sealed class MqttBridge : IMqttBridge
{
    private readonly MqttBroker _localBroker;
    private readonly MqttBridgeOptions _options;
    private MqttClient? _remoteClient;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private bool _isStarted;

    // 统计信息
    private long _upstreamMessageCount;
    private long _downstreamMessageCount;
    private long _upstreamByteCount;
    private long _downstreamByteCount;
    private long _upstreamFailedCount;
    private long _downstreamFailedCount;
    private int _reconnectCount;
    private DateTime? _connectedAt;
    private DateTime? _lastActivityAt;

    /// <summary>
    /// 获取桥接名称。
    /// </summary>
    public string Name => _options.Name;

    /// <summary>
    /// 获取桥接是否已连接。
    /// </summary>
    public bool IsConnected => _remoteClient?.IsConnected ?? false;

    /// <summary>
    /// 获取桥接配置。
    /// </summary>
    public MqttBridgeOptions Options => _options;

    /// <summary>
    /// 当桥接连接成功时触发。
    /// </summary>
    public event EventHandler<MqttBridgeConnectedEventArgs>? Connected;

    /// <summary>
    /// 当桥接断开连接时触发。
    /// </summary>
    public event EventHandler<MqttBridgeDisconnectedEventArgs>? Disconnected;

    /// <summary>
    /// 当桥接消息转发时触发。
    /// </summary>
    public event EventHandler<MqttBridgeMessageForwardedEventArgs>? MessageForwarded;

    /// <summary>
    /// 使用本地 Broker 和配置创建桥接。
    /// </summary>
    /// <param name="localBroker">本地 Broker</param>
    /// <param name="options">桥接配置</param>
    public MqttBridge(MqttBroker localBroker, MqttBridgeOptions options)
    {
        _localBroker = localBroker ?? throw new ArgumentNullException(nameof(localBroker));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 启动桥接连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_isStarted) return;

        _cts = new CancellationTokenSource();

        // 创建远程客户端
        var clientOptions = new MqttClientOptions
        {
            Host = _options.RemoteHost,
            Port = _options.RemotePort,
            ClientId = _options.ClientId,
            Username = _options.Username,
            Password = _options.Password,
            UseTls = _options.UseTls,
            ProtocolVersion = _options.ProtocolVersion,
            KeepAliveSeconds = _options.KeepAliveSeconds,
            AutoReconnect = true,
            ReconnectDelayMs = _options.ReconnectDelayMs,
            CleanSession = true,
            ConnectionTimeoutSeconds = _options.ConnectionTimeoutSeconds
        };

        _remoteClient = new MqttClient(clientOptions);

        // 注册远程客户端事件
        _remoteClient.Connected += OnRemoteClientConnected;
        _remoteClient.Disconnected += OnRemoteClientDisconnected;
        _remoteClient.MessageReceived += OnRemoteMessageReceived;

        // 注册本地 Broker 事件（用于上行同步）
        _localBroker.MessagePublished += OnLocalMessagePublished;

        // 连接到远程 Broker
        var result = await _remoteClient.ConnectAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            throw new MqttBridgeConnectionException(
                $"无法连接到远程 Broker: {result.ResultCode}",
                $"{_options.RemoteHost}:{_options.RemotePort}");
        }

        _isStarted = true;
        _connectedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 停止桥接连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted) return;

        _isStarted = false;

        // 取消订阅事件
        _localBroker.MessagePublished -= OnLocalMessagePublished;

        if (_remoteClient != null)
        {
            _remoteClient.Connected -= OnRemoteClientConnected;
            _remoteClient.Disconnected -= OnRemoteClientDisconnected;
            _remoteClient.MessageReceived -= OnRemoteMessageReceived;

            try
            {
                await _remoteClient.DisconnectAsync(cancellationToken);
            }
            catch
            {
                // 忽略断开时的错误
            }
        }

        _cts?.Cancel();
    }

    /// <summary>
    /// 远程客户端连接成功时，订阅下行主题并同步保留消息。
    /// </summary>
    private async void OnRemoteClientConnected(object? sender, MqttConnectedEventArgs e)
    {
        if (_reconnectCount > 0)
        {
            Interlocked.Increment(ref _reconnectCount);
        }
        _connectedAt = DateTime.UtcNow;

        Connected?.Invoke(this, new MqttBridgeConnectedEventArgs
        {
            BridgeName = Name,
            ConnectedAt = DateTime.UtcNow,
            RemoteEndpoint = $"{_options.RemoteHost}:{_options.RemotePort}"
        });

        // 订阅下行规则中的所有主题（会自动接收远程的保留消息）
        foreach (var rule in _options.DownstreamRules.Where(r => r.Enabled))
        {
            try
            {
                var qos = rule.QualityOfService ?? _options.QualityOfService;
                await _remoteClient!.SubscribeAsync(rule.LocalTopicFilter, qos);
            }
            catch (Exception)
            {
                // 记录订阅失败，但不中断
            }
        }

        // 同步本地保留消息到远程（如果匹配上行规则）
        await SyncRetainedMessagesUpstreamAsync();
    }

    /// <summary>
    /// 同步本地保留消息到远程 Broker。
    /// </summary>
    private async Task SyncRetainedMessagesUpstreamAsync()
    {
        if (!IsConnected || !_options.SyncRetainedMessages) return;

        var retainedMessages = _localBroker.RetainedMessages;
        foreach (var kvp in retainedMessages)
        {
            var message = kvp.Value;

            // 检查是否匹配上行规则
            foreach (var rule in _options.UpstreamRules.Where(r => r.Enabled))
            {
                if (TopicMatches(rule.LocalTopicFilter, message.Topic))
                {
                    try
                    {
                        var remoteTopic = TransformTopicUpstream(message.Topic, rule);
                        var remoteMessage = new MqttApplicationMessage
                        {
                            Topic = remoteTopic,
                            Payload = message.Payload,
                            QualityOfService = rule.QualityOfService ?? _options.QualityOfService,
                            Retain = true // 保留消息必须设置 Retain 标志
                        };

                        await _remoteClient!.PublishAsync(remoteMessage);

                        Interlocked.Increment(ref _upstreamMessageCount);
                        Interlocked.Add(ref _upstreamByteCount, message.Payload.Length);
                    }
                    catch
                    {
                        // 忽略同步失败
                    }

                    break;
                }
            }
        }
    }

    /// <summary>
    /// 远程客户端断开连接时触发。
    /// </summary>
    private void OnRemoteClientDisconnected(object? sender, MqttDisconnectedEventArgs e)
    {
        Disconnected?.Invoke(this, new MqttBridgeDisconnectedEventArgs
        {
            BridgeName = Name,
            Exception = e.Exception,
            WillReconnect = e.WillReconnect,
            DisconnectedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 处理从远程 Broker 收到的消息（下行同步：远程 -> 本地）。
    /// </summary>
    private async void OnRemoteMessageReceived(object? sender, MqttMessageReceivedEventArgs e)
    {
        if (!_isStarted) return;

        var message = e.Message;
        _lastActivityAt = DateTime.UtcNow;

        // 检查是否匹配下行规则
        foreach (var rule in _options.DownstreamRules.Where(r => r.Enabled))
        {
            if (TopicMatches(rule.LocalTopicFilter, message.Topic))
            {
                try
                {
                    // 转换主题
                    var localTopic = TransformTopicDownstream(message.Topic, rule);

                    var localMessage = new MqttApplicationMessage
                    {
                        Topic = localTopic,
                        Payload = message.Payload,
                        QualityOfService = rule.QualityOfService ?? message.QualityOfService,
                        Retain = _options.SyncRetainFlag ? message.Retain : false
                    };

                    // 发布到本地 Broker
                    await _localBroker.PublishAsync(localMessage);

                    Interlocked.Increment(ref _downstreamMessageCount);
                    Interlocked.Add(ref _downstreamByteCount, message.Payload.Length);

                    MessageForwarded?.Invoke(this, new MqttBridgeMessageForwardedEventArgs
                    {
                        BridgeName = Name,
                        Direction = BridgeDirection.Downstream,
                        OriginalTopic = message.Topic,
                        TransformedTopic = localTopic,
                        PayloadSize = message.Payload.Length
                    });
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref _downstreamFailedCount);
                }

                break; // 只匹配第一个规则
            }
        }
    }

    /// <summary>
    /// 处理本地 Broker 发布的消息（上行同步：本地 -> 远程）。
    /// </summary>
    private async void OnLocalMessagePublished(object? sender, MqttMessagePublishedEventArgs e)
    {
        if (!_isStarted || !IsConnected) return;

        var message = e.Message;
        _lastActivityAt = DateTime.UtcNow;

        // 检查是否匹配上行规则
        foreach (var rule in _options.UpstreamRules.Where(r => r.Enabled))
        {
            if (TopicMatches(rule.LocalTopicFilter, message.Topic))
            {
                try
                {
                    // 转换主题（添加远程前缀）
                    var remoteTopic = TransformTopicUpstream(message.Topic, rule);

                    var remoteMessage = new MqttApplicationMessage
                    {
                        Topic = remoteTopic,
                        Payload = message.Payload,
                        QualityOfService = rule.QualityOfService ?? _options.QualityOfService,
                        Retain = _options.SyncRetainFlag ? message.Retain : false
                    };

                    await _remoteClient!.PublishAsync(remoteMessage);

                    Interlocked.Increment(ref _upstreamMessageCount);
                    Interlocked.Add(ref _upstreamByteCount, message.Payload.Length);

                    MessageForwarded?.Invoke(this, new MqttBridgeMessageForwardedEventArgs
                    {
                        BridgeName = Name,
                        Direction = BridgeDirection.Upstream,
                        OriginalTopic = message.Topic,
                        TransformedTopic = remoteTopic,
                        PayloadSize = message.Payload.Length
                    });
                }
                catch (Exception)
                {
                    Interlocked.Increment(ref _upstreamFailedCount);
                    // 断开时丢弃消息，不缓存
                }

                break; // 只匹配第一个规则
            }
        }
    }

    /// <summary>
    /// 上行主题转换（添加远程前缀）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string TransformTopicUpstream(string localTopic, MqttBridgeRule rule)
    {
        if (string.IsNullOrEmpty(rule.RemoteTopicPrefix))
            return localTopic;

        return rule.RemoteTopicPrefix + localTopic;
    }

    /// <summary>
    /// 下行主题转换（移除远程前缀或添加本地前缀）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string TransformTopicDownstream(string remoteTopic, MqttBridgeRule rule)
    {
        var topic = remoteTopic;

        // 移除远程前缀
        if (!string.IsNullOrEmpty(rule.RemoteTopicPrefix) && topic.StartsWith(rule.RemoteTopicPrefix))
        {
            topic = topic[rule.RemoteTopicPrefix.Length..];
        }

        // 添加本地前缀
        if (!string.IsNullOrEmpty(rule.LocalTopicPrefix))
        {
            topic = rule.LocalTopicPrefix + topic;
        }

        return topic;
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
    /// 获取桥接统计信息。
    /// </summary>
    public MqttBridgeStatistics GetStatistics()
    {
        return new MqttBridgeStatistics
        {
            BridgeName = Name,
            IsConnected = IsConnected,
            ConnectedAt = _connectedAt,
            UpstreamMessageCount = Interlocked.Read(ref _upstreamMessageCount),
            DownstreamMessageCount = Interlocked.Read(ref _downstreamMessageCount),
            UpstreamByteCount = Interlocked.Read(ref _upstreamByteCount),
            DownstreamByteCount = Interlocked.Read(ref _downstreamByteCount),
            UpstreamFailedCount = Interlocked.Read(ref _upstreamFailedCount),
            DownstreamFailedCount = Interlocked.Read(ref _downstreamFailedCount),
            ReconnectCount = _reconnectCount,
            LastActivityAt = _lastActivityAt
        };
    }

    /// <summary>
    /// 检查对象是否已释放。
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttBridge));
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await StopAsync();
        _remoteClient?.Dispose();
        _cts?.Dispose();
    }
}
