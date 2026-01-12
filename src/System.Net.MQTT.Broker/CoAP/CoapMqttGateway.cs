using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.MQTT.Broker.Transport;
using System.Net.MQTT.Broker.Transport.Udp;
using System.Net.MQTT.CoAP.Protocol;
using System.Net.MQTT.CoAP.Serialization;
using System.Net.MQTT.Serialization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace System.Net.MQTT.Broker.CoAP;

/// <summary>
/// CoAP 观察者信息。
/// </summary>
internal sealed class CoapObserver
{
    public ITransportConnection Connection { get; init; } = null!;
    public byte[] Token { get; init; } = Array.Empty<byte>();
    public string Topic { get; init; } = string.Empty;
    public uint SequenceNumber { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// CoAP-MQTT 网关。
/// 将 CoAP 请求映射到 MQTT 操作：
/// - GET /mqtt/topic → 返回保留消息
/// - GET /mqtt/topic + Observe → 订阅主题
/// - PUT /mqtt/topic → 发布消息
/// - DELETE /mqtt/topic → 删除保留消息
/// </summary>
public sealed class CoapMqttGateway : IAsyncDisposable
{
    private readonly MqttBroker _broker;
    private readonly MqttBrokerOptions _options;
    private readonly ILogger<CoapMqttGateway>? _logger;
    private readonly ConcurrentDictionary<string, CoapObserver> _observers = new();

    private UdpTransportListener? _udpListener;
    private CancellationTokenSource? _cts;
    private Task? _acceptTask;
    private ushort _messageIdCounter;
    private volatile bool _disposed;

    /// <summary>
    /// 创建 CoAP-MQTT 网关。
    /// </summary>
    /// <param name="broker">MQTT Broker 实例</param>
    /// <param name="options">配置选项</param>
    /// <param name="logger">日志记录器</param>
    public CoapMqttGateway(MqttBroker broker, MqttBrokerOptions options, ILogger<CoapMqttGateway>? logger = null)
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
            return;
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // 创建并启动 UDP 监听器
        var bindAddress = IPAddress.Parse(_options.BindAddress);
        _udpListener = new UdpTransportListener(bindAddress, _options.CoapPort)
        {
            ConnectionTimeout = TimeSpan.FromSeconds(_options.CoapSessionTimeoutSeconds)
        };
        _udpListener.Start();

        _logger?.LogInformation("CoAP-MQTT 网关已启动，端口: {Port}", _options.CoapPort);

        // 启动接受连接任务
        _acceptTask = AcceptClientsAsync(_cts.Token);
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

        // 清理观察者
        _observers.Clear();

        // 停止监听器
        if (_udpListener != null)
        {
            _udpListener.Stop();
            await _udpListener.DisposeAsync();
            _udpListener = null;
        }

        try
        {
            if (_acceptTask != null)
            {
                await _acceptTask;
            }
        }
        catch (OperationCanceledException)
        {
            // 预期的取消
        }

        _cts.Dispose();
        _cts = null;

        _logger?.LogInformation("CoAP-MQTT 网关已停止");
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
                _logger?.LogError(ex, "接受 CoAP 客户端连接时发生错误");
            }
        }
    }

    /// <summary>
    /// 处理客户端连接。
    /// </summary>
    private async Task HandleClientAsync(ITransportConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && connection.IsConnected)
            {
                var datagram = await connection.ReadDatagramAsync(cancellationToken);
                if (datagram.IsEmpty)
                {
                    break;
                }

                try
                {
                    var message = CoapSerializer.Deserialize(datagram.Span);
                    await ProcessMessageAsync(connection, message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "解析 CoAP 消息失败");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "处理 CoAP 客户端时发生错误");
        }
    }

    /// <summary>
    /// 处理 CoAP 消息。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task ProcessMessageAsync(
        ITransportConnection connection,
        CoapMessage message,
        CancellationToken cancellationToken)
    {
        // 处理 ACK 和 RST
        if (message.Type == CoapMessageType.Acknowledgement || message.Type == CoapMessageType.Reset)
        {
            return;
        }

        // 处理请求
        if (message.IsRequest)
        {
            await HandleRequestAsync(connection, message, cancellationToken);
        }
    }

    /// <summary>
    /// 处理 CoAP 请求。
    /// </summary>
    private async Task HandleRequestAsync(
        ITransportConnection connection,
        CoapMessage request,
        CancellationToken cancellationToken)
    {
        var uriPath = request.GetUriPath();

        // 检查路径前缀
        if (!uriPath.StartsWith(_options.CoapMqttPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await SendErrorResponseAsync(connection, request, CoapCode.NotFound, cancellationToken);
            return;
        }

        // 提取 MQTT 主题
        var topic = uriPath.Substring(_options.CoapMqttPrefix.Length).TrimStart('/');
        if (string.IsNullOrEmpty(topic))
        {
            await SendErrorResponseAsync(connection, request, CoapCode.BadRequest, cancellationToken);
            return;
        }

        _logger?.LogDebug("CoAP 请求: {Method} /{Prefix}/{Topic}",
            request.Code, _options.CoapMqttPrefix, topic);

        // 根据方法处理
        if (request.Code == CoapCode.Get)
        {
            await HandleGetAsync(connection, request, topic, cancellationToken);
        }
        else if (request.Code == CoapCode.Put || request.Code == CoapCode.Post)
        {
            await HandlePutAsync(connection, request, topic, cancellationToken);
        }
        else if (request.Code == CoapCode.Delete)
        {
            await HandleDeleteAsync(connection, request, topic, cancellationToken);
        }
        else
        {
            await SendErrorResponseAsync(connection, request, CoapCode.MethodNotAllowed, cancellationToken);
        }
    }

    /// <summary>
    /// 处理 GET 请求。
    /// </summary>
    private async Task HandleGetAsync(
        ITransportConnection connection,
        CoapMessage request,
        string topic,
        CancellationToken cancellationToken)
    {
        var observe = request.GetObserve();

        if (observe.HasValue && observe.Value == 0)
        {
            // 注册观察者（订阅）
            await RegisterObserverAsync(connection, request, topic, cancellationToken);
        }
        else if (observe.HasValue && observe.Value == 1)
        {
            // 注销观察者
            UnregisterObserver(connection, request.Token, topic);
            await SendResponseAsync(connection, request, CoapCode.Deleted, null, cancellationToken);
        }
        else
        {
            // 普通 GET - 返回保留消息
            var retainedMessage = _broker.GetRetainedMessage(topic);
            if (retainedMessage != null)
            {
                var response = CoapMessage.CreateResponse(request, CoapCode.Content);
                response.Payload = retainedMessage.Payload.ToArray();
                response.SetContentFormat(CoapContentFormat.OctetStream);
                await SendMessageAsync(connection, response, cancellationToken);
            }
            else
            {
                await SendErrorResponseAsync(connection, request, CoapCode.NotFound, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 处理 PUT 请求（发布消息）。
    /// </summary>
    private async Task HandlePutAsync(
        ITransportConnection connection,
        CoapMessage request,
        string topic,
        CancellationToken cancellationToken)
    {
        // 检查消息大小
        if (request.Payload.Length > _broker.Options.MaxMessageSize)
        {
            // 消息过大
            await SendErrorResponseAsync(connection, request, CoapCode.RequestEntityTooLarge, cancellationToken);
            return;
        }

        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = request.Payload,
            QualityOfService = MqttQualityOfService.AtMostOnce,
            Retain = true // PUT 默认保留
        };

        var sourceClientId = connection.RemoteEndPoint?.ToString() ?? connection.ConnectionId;
        await _broker.PublishAsync(message, MqttProtocolType.CoAP, sourceClientId, cancellationToken);

        var response = CoapMessage.CreateResponse(request, CoapCode.Changed);
        await SendMessageAsync(connection, response, cancellationToken);

        _logger?.LogDebug("CoAP PUT 已发布到 MQTT: {Topic}", topic);
    }

    /// <summary>
    /// 处理 DELETE 请求（删除保留消息）。
    /// </summary>
    private async Task HandleDeleteAsync(
        ITransportConnection connection,
        CoapMessage request,
        string topic,
        CancellationToken cancellationToken)
    {
        // 发布空消息以删除保留消息
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = Array.Empty<byte>(),
            QualityOfService = MqttQualityOfService.AtMostOnce,
            Retain = true
        };

        var sourceClientId = connection.RemoteEndPoint?.ToString() ?? connection.ConnectionId;
        await _broker.PublishAsync(message, MqttProtocolType.CoAP, sourceClientId, cancellationToken);

        var response = CoapMessage.CreateResponse(request, CoapCode.Deleted);
        await SendMessageAsync(connection, response, cancellationToken);

        _logger?.LogDebug("CoAP DELETE 已删除保留消息: {Topic}", topic);
    }

    /// <summary>
    /// 注册观察者。
    /// </summary>
    private async Task RegisterObserverAsync(
        ITransportConnection connection,
        CoapMessage request,
        string topic,
        CancellationToken cancellationToken)
    {
        var observerKey = $"{connection.ConnectionId}:{Convert.ToBase64String(request.Token)}:{topic}";

        var observer = new CoapObserver
        {
            Connection = connection,
            Token = request.Token,
            Topic = topic,
            SequenceNumber = 0
        };

        _observers[observerKey] = observer;

        // 发送初始响应
        var retainedMessage = _broker.GetRetainedMessage(topic);
        var response = CoapMessage.CreateResponse(request, CoapCode.Content);
        response.SetObserve(observer.SequenceNumber++);

        if (retainedMessage != null)
        {
            response.Payload = retainedMessage.Payload.ToArray();
            response.SetContentFormat(CoapContentFormat.OctetStream);
        }

        await SendMessageAsync(connection, response, cancellationToken);

        _logger?.LogDebug("CoAP 观察者已注册: {Topic}", topic);
    }

    /// <summary>
    /// 注销观察者。
    /// </summary>
    private void UnregisterObserver(ITransportConnection connection, byte[] token, string topic)
    {
        var observerKey = $"{connection.ConnectionId}:{Convert.ToBase64String(token)}:{topic}";
        _observers.TryRemove(observerKey, out _);
        _logger?.LogDebug("CoAP 观察者已注销: {Topic}", topic);
    }

    /// <summary>
    /// 通知观察者有新消息。
    /// 由 Broker 在消息发布时调用。
    /// </summary>
    /// <param name="topic">主题</param>
    /// <param name="payload">有效载荷</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task NotifyObserversAsync(string topic, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        foreach (var kvp in _observers)
        {
            var observer = kvp.Value;
            if (!TopicMatches(observer.Topic, topic))
            {
                continue;
            }

            try
            {
                var notification = new CoapMessage
                {
                    Type = CoapMessageType.NonConfirmable,
                    Code = CoapCode.Content,
                    MessageId = GetNextMessageId(),
                    Token = observer.Token
                };
                notification.SetObserve(observer.SequenceNumber++);
                notification.Payload = payload.ToArray();
                notification.SetContentFormat(CoapContentFormat.OctetStream);

                await SendMessageAsync(observer.Connection, notification, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "通知 CoAP 观察者失败: {Topic}", topic);
                // 移除失败的观察者
                _observers.TryRemove(kvp.Key, out _);
            }
        }
    }

    /// <summary>
    /// 发送错误响应。
    /// </summary>
    private async Task SendErrorResponseAsync(
        ITransportConnection connection,
        CoapMessage request,
        CoapCode code,
        CancellationToken cancellationToken)
    {
        var response = CoapMessage.CreateResponse(request, code);
        await SendMessageAsync(connection, response, cancellationToken);
    }

    /// <summary>
    /// 发送响应。
    /// </summary>
    private async Task SendResponseAsync(
        ITransportConnection connection,
        CoapMessage request,
        CoapCode code,
        byte[]? payload,
        CancellationToken cancellationToken)
    {
        var response = CoapMessage.CreateResponse(request, code);
        if (payload != null)
        {
            response.Payload = payload;
        }
        await SendMessageAsync(connection, response, cancellationToken);
    }

    /// <summary>
    /// 发送 CoAP 消息。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task SendMessageAsync(
        ITransportConnection connection,
        CoapMessage message,
        CancellationToken cancellationToken)
    {
        var length = CoapSerializer.SerializeWithPooledBuffer(message, out var buffer);
        try
        {
            await connection.WriteAsync(buffer.AsMemory(0, length), cancellationToken);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// 获取下一个消息 ID。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort GetNextMessageId()
    {
        return _messageIdCounter++;
    }

    /// <summary>
    /// 检查主题是否匹配过滤器。
    /// </summary>
    private static bool TopicMatches(string filter, string topic)
    {
        // 简单实现，支持通配符
        if (filter == topic) return true;
        if (filter.EndsWith("/#"))
        {
            var prefix = filter.Substring(0, filter.Length - 2);
            return topic.StartsWith(prefix);
        }
        return false;
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
            throw new ObjectDisposedException(nameof(CoapMqttGateway));
        }
    }
}
