using System.Buffers;
using System.Net.MQTT.Protocol;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace System.Net.MQTT;

/// <summary>
/// MQTT 客户端实现。
/// 支持 MQTT 3.1.1 和 MQTT 5.0 协议，提供连接、发布、订阅等功能。
/// </summary>
public sealed class MqttClient : IMqttClient
{
    /// <summary>
    /// 默认缓冲区大小（4KB）
    /// </summary>
    private const int DefaultBufferSize = 4096;

    /// <summary>
    /// 最大剩余长度字节数（MQTT 协议规定最多 4 字节）
    /// </summary>
    private const int MaxRemainingLengthBytes = 4;

    private TcpClient? _tcpClient;
    private Stream? _stream;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private ushort _packetId;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Dictionary<ushort, TaskCompletionSource<object?>> _pendingPackets = new(16);
    private readonly object _pendingLock = new();
    private bool _disposed;
    private Timer? _keepAliveTimer;
    private IMqttProtocolHandler? _protocolHandler;

    /// <inheritdoc/>
    public bool IsConnected { get; private set; }

    /// <inheritdoc/>
    public MqttClientOptions Options { get; }

    /// <inheritdoc/>
    public event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

    /// <inheritdoc/>
    public event EventHandler<MqttConnectedEventArgs>? Connected;

    /// <inheritdoc/>
    public event EventHandler<MqttDisconnectedEventArgs>? Disconnected;

    /// <summary>
    /// 使用指定的配置创建 MQTT 客户端。
    /// </summary>
    /// <param name="options">客户端配置选项</param>
    /// <exception cref="ArgumentNullException">当 options 为 null 时抛出</exception>
    public MqttClient(MqttClientOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 使用指定的主机和端口创建 MQTT 客户端。
    /// </summary>
    /// <param name="host">MQTT 服务器主机名或 IP 地址</param>
    /// <param name="port">MQTT 服务器端口，默认 1883</param>
    public MqttClient(string host, int port = 1883) : this(new MqttClientOptions { Host = host, Port = port })
    {
    }

    /// <inheritdoc/>
    public async Task<MqttConnectResult> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (IsConnected)
        {
            return new MqttConnectResult { ResultCode = MqttConnectResultCode.Success };
        }

        // 初始化协议处理器
        _protocolHandler = MqttProtocolHandlerFactory.GetHandler(Options.ProtocolVersion);

        _tcpClient = new TcpClient();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Options.ConnectionTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        await _tcpClient.ConnectAsync(Options.Host, Options.Port, linkedCts.Token).ConfigureAwait(false);

        if (Options.UseTls)
        {
            var sslStream = new SslStream(
                _tcpClient.GetStream(),
                false,
                Options.SkipCertificateValidation ? (_, _, _, _) => true : null);

            var sslOptions = new SslClientAuthenticationOptions
            {
                TargetHost = Options.Host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            };

            if (Options.ClientCertificate != null)
            {
                sslOptions.ClientCertificates = new X509CertificateCollection { Options.ClientCertificate };
            }

            await sslStream.AuthenticateAsClientAsync(sslOptions, linkedCts.Token).ConfigureAwait(false);
            _stream = sslStream;
        }
        else
        {
            _stream = _tcpClient.GetStream();
        }

        // 使用协议处理器构建 CONNECT 报文
        var connectPacket = _protocolHandler.ConnectBuilder.CreateFromOptions(Options);
        await SendPacketAsync(connectPacket, linkedCts.Token).ConfigureAwait(false);

        // 接收并解析 CONNACK 响应
        var (packetType, flags, payload) = await ReceiveAndParsePacketAsync(linkedCts.Token).ConfigureAwait(false);

        if (packetType != MqttPacketType.ConnAck)
        {
            throw new MqttProtocolException("无效的 CONNACK 响应");
        }

        var connAckPacket = (MqttConnAckPacket)_protocolHandler.ParsePacket(packetType, flags, payload.Span);

        var result = new MqttConnectResult
        {
            ResultCode = (MqttConnectResultCode)connAckPacket.ReasonCode,
            SessionPresent = connAckPacket.SessionPresent
        };

        if (result.IsSuccess)
        {
            IsConnected = true;
            StartReceiveLoop();
            StartKeepAlive();
            Connected?.Invoke(this, new MqttConnectedEventArgs { Result = result });
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected) return;

        try
        {
            // 使用协议处理器构建 DISCONNECT 报文
            var disconnectPacket = _protocolHandler!.DisconnectBuilder.Create();
            await SendPacketAsync(disconnectPacket, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await CleanupAsync(clientInitiated: true).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public Task PublishAsync(string topic, string payload, MqttQualityOfService qos = MqttQualityOfService.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default)
    {
        return PublishAsync(MqttApplicationMessage.Create(topic, payload, qos, retain), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task PublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            throw new InvalidOperationException("客户端未连接");
        }

        // 获取报文标识符（QoS > 0 时需要）
        ushort packetId = 0;
        if (message.QualityOfService != MqttQualityOfService.AtMostOnce)
        {
            packetId = GetNextPacketId();
        }

        // 使用协议处理器构建 PUBLISH 报文
        var packet = _protocolHandler!.PublishBuilder.CreateFromMessage(message, packetId);
        await SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);

        // QoS 1 和 QoS 2 需要等待确认
        if (message.QualityOfService == MqttQualityOfService.AtLeastOnce ||
            message.QualityOfService == MqttQualityOfService.ExactlyOnce)
        {
            await WaitForPacketAsync(packetId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public Task<MqttSubscribeResult> SubscribeAsync(string topic, MqttQualityOfService qos = MqttQualityOfService.AtMostOnce, CancellationToken cancellationToken = default)
    {
        return SubscribeAsync(new[] { MqttTopicSubscription.Create(topic, qos) }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MqttSubscribeResult> SubscribeAsync(IEnumerable<MqttTopicSubscription> subscriptions, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            throw new InvalidOperationException("客户端未连接");
        }

        var subList = subscriptions.ToList();
        var packetId = GetNextPacketId();

        // 使用协议处理器构建 SUBSCRIBE 报文
        var packet = _protocolHandler!.SubscribeBuilder.Create(packetId, subList);
        await SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);

        var response = await WaitForPacketAsync(packetId, cancellationToken).ConfigureAwait(false) as byte[];
        var results = response?.Select(b => (MqttSubscribeResultCode)b).ToList() ?? new List<MqttSubscribeResultCode>();

        return new MqttSubscribeResult { Results = results };
    }

    /// <inheritdoc/>
    public Task<MqttUnsubscribeResult> UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        return UnsubscribeAsync(new[] { topic }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<MqttUnsubscribeResult> UnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            throw new InvalidOperationException("客户端未连接");
        }

        var topicList = topics.ToList();
        var packetId = GetNextPacketId();

        // 使用协议处理器构建 UNSUBSCRIBE 报文
        var packet = _protocolHandler!.UnsubscribeBuilder.Create(packetId, topicList);
        await SendPacketAsync(packet, cancellationToken).ConfigureAwait(false);

        await WaitForPacketAsync(packetId, cancellationToken).ConfigureAwait(false);
        return new MqttUnsubscribeResult { IsSuccess = true };
    }

    /// <summary>
    /// 检查对象是否已释放，如果已释放则抛出异常。
    /// 使用 NoInlining 避免将异常抛出代码内联到热路径。
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MqttClient));
        }
    }

    /// <summary>
    /// 发送报文对象到服务器。
    /// 使用协议处理器序列化报文。
    /// </summary>
    /// <param name="packet">要发送的报文对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SendPacketAsync(IMqttPacket packet, CancellationToken cancellationToken)
    {
        // 使用 ArrayBufferWriter 来构建报文
        var writer = new ArrayBufferWriter<byte>(256);
        _protocolHandler!.WritePacket(packet, writer);
        await SendPacketBytesAsync(writer.WrittenSpan.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 发送原始字节数组到服务器。
    /// 使用 Memory 进行异步写入，减少内存分配。
    /// </summary>
    /// <param name="packet">要发送的报文字节</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SendPacketBytesAsync(byte[] packet, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_stream != null)
            {
                // 使用 Memory<byte> 进行异步写入
                await _stream.WriteAsync(packet.AsMemory(), cancellationToken).ConfigureAwait(false);
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 从服务器接收报文。
    /// 使用 ArrayPool 减少内存分配。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接收到的报文</returns>
    private async Task<byte[]> ReceivePacketAsync(CancellationToken cancellationToken)
    {
        if (_stream == null) return Array.Empty<byte>();

        // 使用 stackalloc 读取固定头部（1 字节）
        var fixedHeaderBuffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            await ReadExactlyAsync(_stream, fixedHeaderBuffer, 0, 1, cancellationToken).ConfigureAwait(false);
            var fixedHeader = fixedHeaderBuffer[0];

            // 从流中解码剩余长度
            var remainingLength = await DecodeRemainingLengthAsync(_stream, cancellationToken).ConfigureAwait(false);

            // 从池中租借有效载荷缓冲区
            var payloadBuffer = ArrayPool<byte>.Shared.Rent(remainingLength > 0 ? remainingLength : 1);
            try
            {
                if (remainingLength > 0)
                {
                    await ReadExactlyAsync(_stream, payloadBuffer, 0, remainingLength, cancellationToken).ConfigureAwait(false);
                }

                // 编码剩余长度
                Span<byte> remainingLengthBytes = stackalloc byte[MaxRemainingLengthBytes];
                var remainingLengthSize = EncodeRemainingLength(remainingLength, remainingLengthBytes);

                // 构建完整报文
                var packet = new byte[1 + remainingLengthSize + remainingLength];
                packet[0] = fixedHeader;
                remainingLengthBytes.Slice(0, remainingLengthSize).CopyTo(packet.AsSpan(1));

                if (remainingLength > 0)
                {
                    payloadBuffer.AsSpan(0, remainingLength).CopyTo(packet.AsSpan(1 + remainingLengthSize));
                }

                return packet;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(payloadBuffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(fixedHeaderBuffer);
        }
    }

    /// <summary>
    /// 接收并解析报文，返回报文类型、标志和有效载荷。
    /// 用于协议处理器解析报文。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>报文类型、标志和有效载荷的元组</returns>
    private async Task<(MqttPacketType packetType, byte flags, ReadOnlyMemory<byte> payload)> ReceiveAndParsePacketAsync(CancellationToken cancellationToken)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("流未初始化");
        }

        // 读取固定头部（1 字节）
        var fixedHeaderBuffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            await ReadExactlyAsync(_stream, fixedHeaderBuffer, 0, 1, cancellationToken).ConfigureAwait(false);
            var fixedHeader = fixedHeaderBuffer[0];

            // 解析报文类型和标志
            var packetType = (MqttPacketType)(fixedHeader >> 4);
            var flags = (byte)(fixedHeader & 0x0F);

            // 解码剩余长度
            var remainingLength = await DecodeRemainingLengthAsync(_stream, cancellationToken).ConfigureAwait(false);

            // 读取有效载荷
            byte[] payload;
            if (remainingLength > 0)
            {
                payload = new byte[remainingLength];
                await ReadExactlyAsync(_stream, payload, 0, remainingLength, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                payload = Array.Empty<byte>();
            }

            return (packetType, flags, payload);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(fixedHeaderBuffer);
        }
    }

    /// <summary>
    /// 从流中精确读取指定字节数。
    /// 循环读取直到获取所需的全部数据。
    /// </summary>
    /// <param name="stream">要读取的流</param>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="offset">缓冲区偏移量</param>
    /// <param name="count">要读取的字节数</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            // 使用 Memory 进行异步读取
            var read = await stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException("连接意外关闭");
            }
            totalRead += read;
        }
    }

    /// <summary>
    /// 启动接收循环。
    /// 在后台任务中持续接收服务器发来的报文。
    /// </summary>
    private void StartReceiveLoop()
    {
        _receiveCts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token));
    }

    /// <summary>
    /// 接收循环主体。
    /// 持续接收并处理报文，直到连接断开或取消。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                var packet = await ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
                if (packet.Length == 0) break;

                var packetType = (MqttPacketType)(packet[0] >> 4);
                await HandlePacketAsync(packetType, packet, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，忽略
        }
        catch (Exception ex)
        {
            await CleanupAsync(clientInitiated: false, ex).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 处理接收到的报文。
    /// 根据报文类型分发到对应的处理方法。
    /// </summary>
    /// <param name="packetType">报文类型</param>
    /// <param name="packet">报文数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task HandlePacketAsync(MqttPacketType packetType, byte[] packet, CancellationToken cancellationToken)
    {
        switch (packetType)
        {
            case MqttPacketType.Publish:
                await HandlePublishAsync(packet, cancellationToken).ConfigureAwait(false);
                break;

            case MqttPacketType.PubAck:
            case MqttPacketType.PubRec:
            case MqttPacketType.PubComp:
            case MqttPacketType.SubAck:
            case MqttPacketType.UnsubAck:
                HandleAcknowledgement(packetType, packet);
                break;

            case MqttPacketType.PubRel:
                await HandlePubRelAsync(packet, cancellationToken).ConfigureAwait(false);
                break;

            case MqttPacketType.PingResp:
                // PINGRESP 不需要处理
                break;
        }
    }

    /// <summary>
    /// 处理接收到的 PUBLISH 报文。
    /// 解析消息内容并触发事件，根据 QoS 发送确认。
    /// </summary>
    /// <param name="packet">PUBLISH 报文数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task HandlePublishAsync(byte[] packet, CancellationToken cancellationToken)
    {
        // 解析固定头部中的标志
        var flags = (byte)(packet[0] & 0x0F);
        var qos = (MqttQualityOfService)((flags >> 1) & 0x03);
        var retain = (flags & 0x01) == 1;
        var dup = (flags & 0x08) != 0;

        // 正确跳过固定头部：1字节类型 + 可变长度的剩余长度
        var index = 1;
        // 解码剩余长度并获取其字节数
        var remainingLengthBytes = 0;
        var multiplier = 1;
        byte encodedByte;
        do
        {
            encodedByte = packet[index++];
            remainingLengthBytes++;
            multiplier *= 128;
        } while ((encodedByte & 128) != 0 && remainingLengthBytes < MaxRemainingLengthBytes);

        // 现在 index 指向可变头部开始位置

        // 读取主题长度（大端序）
        var topicLength = (packet[index] << 8) | packet[index + 1];
        index += 2;

        // 读取主题名称
        var topic = Encoding.UTF8.GetString(packet, index, topicLength);
        index += topicLength;

        // 报文标识符（仅 QoS > 0）
        ushort packetId = 0;
        if (qos != MqttQualityOfService.AtMostOnce)
        {
            packetId = (ushort)((packet[index] << 8) | packet[index + 1]);
            index += 2;
        }

        // MQTT 5.0: 跳过属性长度
        var isV5 = Options.ProtocolVersion == MqttProtocolVersion.V500;
        if (isV5)
        {
            // 解码属性长度（可变长度整数）
            var propertiesLength = 0;
            var propMultiplier = 1;
            do
            {
                encodedByte = packet[index++];
                propertiesLength += (encodedByte & 127) * propMultiplier;
                propMultiplier *= 128;
            } while ((encodedByte & 128) != 0);

            // 跳过属性内容
            index += propertiesLength;
        }

        // 读取有效载荷
        var payloadLength = packet.Length - index;
        var payload = new byte[payloadLength];
        if (payloadLength > 0)
        {
            packet.AsSpan(index, payloadLength).CopyTo(payload);
        }

        // 构建消息对象
        var message = new MqttApplicationMessage
        {
            Topic = topic,
            Payload = payload,
            QualityOfService = qos,
            Retain = retain
        };

        // 触发消息接收事件
        MessageReceived?.Invoke(this, new MqttMessageReceivedEventArgs { Message = message });

        // 根据 QoS 发送确认，使用协议处理器构建响应
        if (qos == MqttQualityOfService.AtLeastOnce)
        {
            // 使用协议处理器构建 PUBACK
            var pubAck = _protocolHandler!.PubAckBuilder.CreatePubAck(packetId);
            await SendPacketAsync(pubAck, cancellationToken).ConfigureAwait(false);
        }
        else if (qos == MqttQualityOfService.ExactlyOnce)
        {
            // 使用协议处理器构建 PUBREC
            var pubRec = _protocolHandler!.PubAckBuilder.CreatePubRec(packetId);
            await SendPacketAsync(pubRec, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 处理 PUBREL 报文（QoS 2 流程的一部分）。
    /// 发送 PUBCOMP 完成消息传递。
    /// </summary>
    /// <param name="packet">PUBREL 报文数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task HandlePubRelAsync(byte[] packet, CancellationToken cancellationToken)
    {
        // 正确跳过固定头部
        var index = SkipFixedHeader(packet);

        // 读取报文标识符
        var packetId = (ushort)((packet[index] << 8) | packet[index + 1]);

        // 使用协议处理器构建 PUBCOMP
        var pubComp = _protocolHandler!.PubAckBuilder.CreatePubComp(packetId);
        await SendPacketAsync(pubComp, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 处理确认报文（PUBACK、PUBREC、PUBCOMP、SUBACK、UNSUBACK）。
    /// 完成等待中的任务。
    /// </summary>
    /// <param name="packetType">报文类型</param>
    /// <param name="packet">报文数据</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleAcknowledgement(MqttPacketType packetType, byte[] packet)
    {
        // 正确跳过固定头部
        var index = SkipFixedHeader(packet);

        // 读取报文标识符
        ushort packetId = (ushort)((packet[index] << 8) | packet[index + 1]);
        index += 2;

        lock (_pendingLock)
        {
            if (_pendingPackets.TryGetValue(packetId, out var tcs))
            {
                if (packetType == MqttPacketType.SubAck)
                {
                    // SUBACK 包含订阅结果代码
                    // MQTT 5.0: 跳过属性长度
                    var isV5 = Options.ProtocolVersion == MqttProtocolVersion.V500;
                    if (isV5)
                    {
                        // 解码属性长度
                        var propertiesLength = 0;
                        var multiplier = 1;
                        byte encodedByte;
                        do
                        {
                            encodedByte = packet[index++];
                            propertiesLength += (encodedByte & 127) * multiplier;
                            multiplier *= 128;
                        } while ((encodedByte & 128) != 0);

                        // 跳过属性
                        index += propertiesLength;
                    }

                    // 读取订阅结果代码
                    var resultsLength = packet.Length - index;
                    var results = new byte[resultsLength];
                    if (resultsLength > 0)
                    {
                        packet.AsSpan(index, resultsLength).CopyTo(results);
                    }
                    tcs.TrySetResult(results);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
                _pendingPackets.Remove(packetId);
            }
        }
    }

    /// <summary>
    /// 跳过固定头部，返回可变头部的起始索引。
    /// </summary>
    /// <param name="packet">报文数据</param>
    /// <returns>可变头部的起始索引</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SkipFixedHeader(byte[] packet)
    {
        var index = 1; // 跳过第一个字节（报文类型+标志）
        var remainingLengthBytes = 0;

        // 解码剩余长度（可变长度编码，1-4字节）
        byte encodedByte;
        do
        {
            encodedByte = packet[index++];
            remainingLengthBytes++;
        } while ((encodedByte & 128) != 0 && remainingLengthBytes < MaxRemainingLengthBytes);

        return index;
    }

    /// <summary>
    /// 启动保活定时器。
    /// 按照配置的间隔发送 PINGREQ 报文。
    /// </summary>
    private void StartKeepAlive()
    {
        if (Options.KeepAliveSeconds > 0)
        {
            // 在保活时间的 75% 处发送 PINGREQ
            var interval = TimeSpan.FromSeconds(Options.KeepAliveSeconds * 0.75);
            _keepAliveTimer = new Timer(async _ =>
            {
                if (IsConnected)
                {
                    try
                    {
                        // 使用协议处理器获取 PINGREQ 报文
                        var pingReq = _protocolHandler!.PingHandler.GetPingReqBytes();
                        await SendPacketBytesAsync(pingReq, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                        // 忽略保活发送失败
                    }
                }
            }, null, interval, interval);
        }
    }

    /// <summary>
    /// 等待指定报文的响应。
    /// 使用 TaskCompletionSource 实现异步等待。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应数据（如果有）</returns>
    private async Task<object?> WaitForPacketAsync(ushort packetId, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_pendingLock)
        {
            _pendingPackets[packetId] = tcs;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 秒超时

        try
        {
            using var registration = cts.Token.Register(() => tcs.TrySetCanceled());
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            lock (_pendingLock)
            {
                _pendingPackets.Remove(packetId);
            }
        }
    }

    /// <summary>
    /// 获取下一个报文标识符。
    /// 标识符范围 1-65535，循环使用。
    /// </summary>
    /// <returns>下一个报文标识符</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort GetNextPacketId()
    {
        // 确保报文 ID 不为 0
        return ++_packetId == 0 ? ++_packetId : _packetId;
    }

    /// <summary>
    /// 将字符串写入缓冲区（包含长度前缀）。
    /// 使用 UTF-8 编码，长度使用大端序。
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="position">写入位置</param>
    /// <param name="value">要写入的字符串</param>
    /// <returns>写入后的位置</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteString(byte[] buffer, int position, string value)
    {
        var length = Encoding.UTF8.GetByteCount(value);
        // 长度前缀（大端序）
        buffer[position++] = (byte)(length >> 8);
        buffer[position++] = (byte)(length & 0xFF);
        // 字符串内容
        Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, position);
        return position + length;
    }

    /// <summary>
    /// 编码 MQTT 剩余长度字段。
    /// 使用可变长度编码，每字节 7 位有效，最高位为延续标志。
    /// </summary>
    /// <param name="length">要编码的长度值</param>
    /// <param name="buffer">输出缓冲区</param>
    /// <returns>编码后的字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EncodeRemainingLength(int length, Span<byte> buffer)
    {
        var index = 0;
        do
        {
            var encodedByte = (byte)(length % 128);
            length /= 128;
            if (length > 0)
            {
                encodedByte |= 0x80; // 设置延续标志
            }
            buffer[index++] = encodedByte;
        } while (length > 0);

        return index;
    }

    /// <summary>
    /// 从流中异步解码 MQTT 剩余长度字段。
    /// </summary>
    /// <param name="stream">输入流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解码后的长度值</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static async Task<int> DecodeRemainingLengthAsync(Stream stream, CancellationToken cancellationToken)
    {
        var multiplier = 1;
        var value = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            byte encodedByte;
            do
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("连接意外关闭");
                }
                encodedByte = buffer[0];
                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;

                // 防止恶意报文导致无限循环
                if (multiplier > 128 * 128 * 128 * 128)
                {
                    throw new MqttProtocolException("剩余长度字段格式错误");
                }
            } while ((encodedByte & 128) != 0);

            return value;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// 清理连接资源。
    /// 停止接收循环，关闭网络连接，触发断开事件。
    /// </summary>
    /// <param name="clientInitiated">是否由客户端主动断开</param>
    /// <param name="exception">导致断开的异常（如果有）</param>
    private async Task CleanupAsync(bool clientInitiated, Exception? exception = null)
    {
        if (!IsConnected && _tcpClient == null) return;

        IsConnected = false;

        // 停止保活定时器
        _keepAliveTimer?.Dispose();
        _keepAliveTimer = null;

        // 取消接收循环
        _receiveCts?.Cancel();

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask.ConfigureAwait(false);
            }
            catch
            {
                // 忽略接收任务的异常
            }
        }

        // 释放网络资源
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _stream = null;
        _tcpClient = null;

        // 触发断开连接事件
        Disconnected?.Invoke(this, new MqttDisconnectedEventArgs
        {
            ClientInitiated = clientInitiated,
            Exception = exception,
            WillReconnect = Options.AutoReconnect && !clientInitiated
        });

        // 自动重连逻辑
        if (Options.AutoReconnect && !clientInitiated && !_disposed)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(Options.ReconnectDelayMs).ConfigureAwait(false);
                if (!_disposed)
                {
                    try
                    {
                        await ConnectAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // 重连失败，将在下次触发
                    }
                }
            });
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _keepAliveTimer?.Dispose();
        _receiveCts?.Cancel();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _sendLock.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await DisconnectAsync().ConfigureAwait(false);
        Dispose();
    }
}
