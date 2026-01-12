using System.Buffers;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace System.Net.MQTT.Broker.Transport.Udp;

/// <summary>
/// UDP 传输虚拟连接实现。
/// 由于 UDP 是无连接协议，此类模拟连接语义。
/// 每个远程端点 (IP:Port) 对应一个虚拟连接。
/// </summary>
public sealed class UdpTransportConnection : ITransportConnection
{
    private readonly UdpTransportListener _listener;
    private readonly IPEndPoint _remoteEndPoint;
    private readonly Channel<byte[]> _receiveChannel;
    private volatile bool _disposed;
    private DateTime _lastActivity;

    /// <summary>
    /// 会话超时时间。
    /// 超过此时间没有活动，连接将被视为已断开。
    /// </summary>
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 创建 UDP 虚拟连接。
    /// </summary>
    /// <param name="listener">所属的 UDP 监听器</param>
    /// <param name="remoteEndPoint">远程端点</param>
    internal UdpTransportConnection(UdpTransportListener listener, IPEndPoint remoteEndPoint)
    {
        _listener = listener ?? throw new ArgumentNullException(nameof(listener));
        _remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        _receiveChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        });
        ConnectionId = $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";
        _lastActivity = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public string ConnectionId { get; }

    /// <inheritdoc/>
    public TransportType TransportType => TransportType.Udp;

    /// <inheritdoc/>
    public EndPoint? RemoteEndPoint => _remoteEndPoint;

    /// <inheritdoc/>
    public EndPoint? LocalEndPoint => _listener.LocalEndPoint;

    /// <inheritdoc/>
    public bool IsConnected => !_disposed && !IsSessionExpired;

    /// <inheritdoc/>
    public bool SupportsStreaming => false;

    /// <summary>
    /// 获取最后活动时间。
    /// </summary>
    public DateTime LastActivity => _lastActivity;

    /// <summary>
    /// 获取会话是否已过期。
    /// </summary>
    public bool IsSessionExpired => DateTime.UtcNow - _lastActivity > SessionTimeout;

    /// <summary>
    /// 将接收到的数据报入队。
    /// 由 UdpTransportListener 调用。
    /// </summary>
    /// <param name="datagram">数据报内容</param>
    internal bool EnqueueDatagram(byte[] datagram)
    {
        if (_disposed) return false;

        _lastActivity = DateTime.UtcNow;
        return _receiveChannel.Writer.TryWrite(datagram);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// UDP 不支持流式读取。此方法将抛出 NotSupportedException。
    /// 请使用 ReadDatagramAsync。
    /// </remarks>
    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("UDP 不支持流式读取，请使用 ReadDatagramAsync。");
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public async ValueTask<ReadOnlyMemory<byte>> ReadDatagramAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var datagram = await _receiveChannel.Reader.ReadAsync(cancellationToken);
            _lastActivity = DateTime.UtcNow;
            return datagram;
        }
        catch (ChannelClosedException)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _listener.SendToAsync(buffer, _remoteEndPoint, cancellationToken);
        _lastActivity = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// UDP 是数据报协议，无需刷新。此方法为空操作。
    /// </remarks>
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        // UDP 无缓冲，无需刷新
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        if (!_disposed)
        {
            _disposed = true;
            _receiveChannel.Writer.TryComplete();
            _listener.RemoveConnection(this);
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return CloseAsync();
    }

    /// <summary>
    /// 更新最后活动时间。
    /// </summary>
    internal void UpdateActivity()
    {
        _lastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查是否已释放。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UdpTransportConnection));
        }
    }
}
