using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace System.Net.MQTT.Broker.Transport.Udp;

/// <summary>
/// UDP 传输监听器实现。
/// 管理 UDP Socket 和虚拟连接。
/// 每个远程端点 (IP:Port) 对应一个虚拟连接。
/// </summary>
public sealed class UdpTransportListener : ITransportListener
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _localEndPoint;
    private readonly ConcurrentDictionary<string, UdpTransportConnection> _connections = new();
    private readonly Channel<UdpTransportConnection> _newConnectionChannel;
    private readonly Timer _cleanupTimer;
    private CancellationTokenSource? _receiveCts;
    private Task? _receiveTask;
    private volatile bool _disposed;

    /// <summary>
    /// 连接超时时间（用于清理过期的虚拟连接）。
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 清理间隔。
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 最大数据报大小。
    /// </summary>
    public int MaxDatagramSize { get; set; } = 65535;

    /// <summary>
    /// 创建 UDP 传输监听器。
    /// </summary>
    /// <param name="endpoint">监听端点</param>
    public UdpTransportListener(IPEndPoint endpoint)
    {
        _localEndPoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _udpClient = new UdpClient(endpoint);
        _newConnectionChannel = Channel.CreateUnbounded<UdpTransportConnection>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _cleanupTimer = new Timer(CleanupExpiredConnections, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// 创建 UDP 传输监听器。
    /// </summary>
    /// <param name="address">监听地址</param>
    /// <param name="port">监听端口</param>
    public UdpTransportListener(IPAddress address, int port)
        : this(new IPEndPoint(address, port))
    {
    }

    /// <inheritdoc/>
    public TransportType TransportType => TransportType.Udp;

    /// <inheritdoc/>
    public EndPoint LocalEndPoint => _localEndPoint;

    /// <inheritdoc/>
    public bool IsListening { get; private set; }

    /// <summary>
    /// 获取当前活跃连接数。
    /// </summary>
    public int ActiveConnections => _connections.Count;

    /// <inheritdoc/>
    public void Start()
    {
        ThrowIfDisposed();

        if (IsListening) return;

        _receiveCts = new CancellationTokenSource();
        _receiveTask = ReceiveLoopAsync(_receiveCts.Token);
        _cleanupTimer.Change(CleanupInterval, CleanupInterval);
        IsListening = true;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!IsListening) return;

        _receiveCts?.Cancel();
        _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);

        // 等待接收任务完成
        try
        {
            _receiveTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // 忽略取消异常
        }

        // 关闭所有连接
        foreach (var connection in _connections.Values)
        {
            _ = connection.CloseAsync();
        }
        _connections.Clear();

        IsListening = false;
    }

    /// <inheritdoc/>
    public async ValueTask<ITransportConnection> AcceptAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        // 等待新连接
        return await _newConnectionChannel.Reader.ReadAsync(cancellationToken);
    }

    /// <summary>
    /// 发送数据到指定端点。
    /// </summary>
    /// <param name="data">要发送的数据</param>
    /// <param name="remoteEndPoint">目标端点</param>
    /// <param name="cancellationToken">取消令牌</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal async ValueTask SendToAsync(ReadOnlyMemory<byte> data, IPEndPoint remoteEndPoint, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _udpClient.SendAsync(data, remoteEndPoint, cancellationToken);
    }

    /// <summary>
    /// 移除连接。
    /// 由 UdpTransportConnection 在关闭时调用。
    /// </summary>
    /// <param name="connection">要移除的连接</param>
    internal void RemoveConnection(UdpTransportConnection connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
    }

    /// <summary>
    /// 接收数据报的主循环。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);
                var remoteEndPoint = result.RemoteEndPoint;
                var connectionId = $"{remoteEndPoint.Address}:{remoteEndPoint.Port}";

                // 获取或创建虚拟连接
                if (!_connections.TryGetValue(connectionId, out var connection))
                {
                    connection = new UdpTransportConnection(this, remoteEndPoint)
                    {
                        SessionTimeout = ConnectionTimeout
                    };

                    if (_connections.TryAdd(connectionId, connection))
                    {
                        // 新连接，通知 AcceptAsync
                        await _newConnectionChannel.Writer.WriteAsync(connection, cancellationToken);
                    }
                    else
                    {
                        // 并发创建，使用已存在的连接
                        connection = _connections[connectionId];
                    }
                }

                // 将数据报入队到对应的虚拟连接
                connection.EnqueueDatagram(result.Buffer);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                break;
            }
            catch
            {
                // 继续接收其他数据报
            }
        }
    }

    /// <summary>
    /// 清理过期的虚拟连接。
    /// </summary>
    private void CleanupExpiredConnections(object? state)
    {
        var expiredConnections = _connections.Values
            .Where(c => c.IsSessionExpired)
            .ToList();

        foreach (var connection in expiredConnections)
        {
            if (_connections.TryRemove(connection.ConnectionId, out _))
            {
                _ = connection.CloseAsync();
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            Stop();
            await _cleanupTimer.DisposeAsync();
            _udpClient.Dispose();
            _newConnectionChannel.Writer.TryComplete();
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
            throw new ObjectDisposedException(nameof(UdpTransportListener));
        }
    }
}
