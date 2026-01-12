using System.Buffers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// 集群对等节点连接。
/// 管理与单个对等节点的 TCP 连接。
/// </summary>
internal sealed class ClusterPeer : IDisposable
{
    private const int HeaderSize = 5;

    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// 获取对等节点信息。
    /// </summary>
    public ClusterPeerInfo Info { get; }

    /// <summary>
    /// 获取连接是否有效。
    /// </summary>
    public bool IsConnected => !_disposed && _tcpClient.Connected;

    /// <summary>
    /// 获取或设置最后心跳时间。
    /// </summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建对等节点连接。
    /// </summary>
    /// <param name="tcpClient">TCP 客户端</param>
    /// <param name="info">节点信息</param>
    public ClusterPeer(TcpClient tcpClient, ClusterPeerInfo info)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        Info = info ?? throw new ArgumentNullException(nameof(info));
        _stream = tcpClient.GetStream();
    }

    /// <summary>
    /// 发送集群消息。
    /// </summary>
    /// <param name="message">集群消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SendAsync(ClusterMessage message, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var data = ClusterSerializer.Serialize(message);
        await SendRawAsync(data, cancellationToken);
    }

    /// <summary>
    /// 发送握手消息。
    /// </summary>
    /// <param name="handshake">握手数据</param>
    /// <param name="isResponse">是否为响应</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task SendHandshakeAsync(ClusterHandshake handshake, bool isResponse, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var data = ClusterSerializer.SerializeHandshake(handshake, isResponse);
        await SendRawAsync(data, cancellationToken);
    }

    /// <summary>
    /// 接收集群消息。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>集群消息，如果连接关闭则返回 null</returns>
    public async Task<ClusterMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // 读取头部
        var headerBuffer = ArrayPool<byte>.Shared.Rent(HeaderSize);
        try
        {
            var read = await ReadExactlyAsync(headerBuffer, 0, HeaderSize, cancellationToken);
            if (read < HeaderSize)
            {
                return null; // 连接关闭
            }

            if (!ClusterSerializer.TryParseHeader(headerBuffer.AsSpan(0, HeaderSize), out var messageType, out var contentLength))
            {
                return null; // 解析失败
            }

            // 读取内容
            if (contentLength > 0)
            {
                var contentBuffer = ArrayPool<byte>.Shared.Rent(contentLength);
                try
                {
                    read = await ReadExactlyAsync(contentBuffer, 0, contentLength, cancellationToken);
                    if (read < contentLength)
                    {
                        return null; // 连接关闭
                    }

                    // 处理握手消息
                    if (messageType == ClusterMessageType.HandshakeRequest || messageType == ClusterMessageType.HandshakeResponse)
                    {
                        var handshake = ClusterSerializer.DeserializeHandshake(contentBuffer.AsSpan(0, contentLength));
                        return new ClusterMessage
                        {
                            Type = messageType,
                            SourceNodeId = handshake.NodeId,
                            Timestamp = handshake.Timestamp
                        };
                    }

                    return ClusterSerializer.Deserialize(messageType, contentBuffer.AsSpan(0, contentLength));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(contentBuffer);
                }
            }

            return new ClusterMessage { Type = messageType };
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    /// <summary>
    /// 接收握手消息。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>握手数据，如果失败则返回 null</returns>
    public async Task<ClusterHandshake?> ReceiveHandshakeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // 读取头部
        var headerBuffer = ArrayPool<byte>.Shared.Rent(HeaderSize);
        try
        {
            var read = await ReadExactlyAsync(headerBuffer, 0, HeaderSize, cancellationToken);
            if (read < HeaderSize)
            {
                return null;
            }

            if (!ClusterSerializer.TryParseHeader(headerBuffer.AsSpan(0, HeaderSize), out var messageType, out var contentLength))
            {
                return null;
            }

            if (messageType != ClusterMessageType.HandshakeRequest && messageType != ClusterMessageType.HandshakeResponse)
            {
                return null;
            }

            // 读取内容
            var contentBuffer = ArrayPool<byte>.Shared.Rent(contentLength);
            try
            {
                read = await ReadExactlyAsync(contentBuffer, 0, contentLength, cancellationToken);
                if (read < contentLength)
                {
                    return null;
                }

                return ClusterSerializer.DeserializeHandshake(contentBuffer.AsSpan(0, contentLength));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(contentBuffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    /// <summary>
    /// 发送原始数据。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task SendRawAsync(byte[] data, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(data.AsMemory(), cancellationToken);
            await _stream.FlushAsync(cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 精确读取指定字节数。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task<int> ReadExactlyAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await _stream.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), cancellationToken);
            if (read == 0)
            {
                break; // 连接关闭
            }
            totalRead += read;
        }
        return totalRead;
    }

    /// <summary>
    /// 检查对象是否已释放。
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ClusterPeer));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _stream.Dispose();
            _tcpClient.Dispose();
        }
        catch
        {
            // 忽略清理时的错误
        }

        _sendLock.Dispose();
    }
}
