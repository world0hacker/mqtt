using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.MQTT.Broker.Transport.Tcp;

/// <summary>
/// TCP 传输连接实现。
/// 封装 TcpClient 和 Stream，提供统一的传输接口。
/// </summary>
public sealed class TcpTransportConnection : ITransportConnection
{
    private readonly TcpClient _tcpClient;
    private readonly Stream _stream;
    private readonly bool _useTls;
    private volatile bool _disposed;

    /// <summary>
    /// 创建 TCP 传输连接。
    /// </summary>
    /// <param name="tcpClient">TCP 客户端</param>
    /// <param name="stream">网络流（可以是 NetworkStream 或 SslStream）</param>
    /// <param name="useTls">是否使用 TLS</param>
    internal TcpTransportConnection(TcpClient tcpClient, Stream stream, bool useTls)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _useTls = useTls;
        ConnectionId = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// 从 TcpClient 创建非 TLS 连接。
    /// </summary>
    public static TcpTransportConnection Create(TcpClient tcpClient)
    {
        return new TcpTransportConnection(tcpClient, tcpClient.GetStream(), false);
    }

    /// <summary>
    /// 从 TcpClient 创建 TLS 连接。
    /// </summary>
    /// <param name="tcpClient">TCP 客户端</param>
    /// <param name="serverCertificate">服务器证书</param>
    /// <param name="requireClientCertificate">是否要求客户端证书</param>
    /// <param name="cancellationToken">取消令牌</param>
    public static async Task<TcpTransportConnection> CreateTlsAsync(
        TcpClient tcpClient,
        X509Certificate2 serverCertificate,
        bool requireClientCertificate = false,
        CancellationToken cancellationToken = default)
    {
        var sslStream = new SslStream(tcpClient.GetStream(), false);
        var sslOptions = new SslServerAuthenticationOptions
        {
            ServerCertificate = serverCertificate,
            ClientCertificateRequired = requireClientCertificate,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };

        await sslStream.AuthenticateAsServerAsync(sslOptions, cancellationToken);
        return new TcpTransportConnection(tcpClient, sslStream, true);
    }

    /// <inheritdoc/>
    public string ConnectionId { get; }

    /// <inheritdoc/>
    public TransportType TransportType => _useTls ? TransportType.TcpTls : TransportType.Tcp;

    /// <inheritdoc/>
    public EndPoint? RemoteEndPoint => _tcpClient.Client.RemoteEndPoint;

    /// <inheritdoc/>
    public EndPoint? LocalEndPoint => _tcpClient.Client.LocalEndPoint;

    /// <inheritdoc/>
    public bool IsConnected => !_disposed && _tcpClient.Connected;

    /// <inheritdoc/>
    public bool SupportsStreaming => true;

    /// <summary>
    /// 获取底层 TcpClient（用于兼容性）。
    /// </summary>
    internal TcpClient TcpClient => _tcpClient;

    /// <summary>
    /// 获取底层 Stream（用于兼容性）。
    /// </summary>
    internal Stream Stream => _stream;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _stream.ReadAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// TCP 是流式协议，此方法不应被调用。
    /// 如果调用，将抛出 NotSupportedException。
    /// </remarks>
    public ValueTask<ReadOnlyMemory<byte>> ReadDatagramAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("TCP 不支持数据报读取，请使用 ReadAsync。");
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _stream.WriteAsync(buffer, cancellationToken);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _stream.FlushAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        if (!_disposed)
        {
            _disposed = true;
            try
            {
                _stream.Dispose();
                _tcpClient.Dispose();
            }
            catch
            {
                // 忽略关闭时的异常
            }
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return CloseAsync();
    }

    /// <summary>
    /// 检查是否已释放。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TcpTransportConnection));
        }
    }
}
