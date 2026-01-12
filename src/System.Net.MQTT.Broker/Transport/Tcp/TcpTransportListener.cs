using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.MQTT.Broker.Transport.Tcp;

/// <summary>
/// TCP 传输监听器实现。
/// 封装 TcpListener，提供统一的监听接口。
/// </summary>
public sealed class TcpTransportListener : ITransportListener
{
    private readonly TcpListener _listener;
    private readonly bool _useTls;
    private readonly X509Certificate2? _serverCertificate;
    private readonly bool _requireClientCertificate;
    private volatile bool _disposed;

    /// <summary>
    /// 创建 TCP 传输监听器。
    /// </summary>
    /// <param name="endpoint">监听端点</param>
    /// <param name="useTls">是否使用 TLS</param>
    /// <param name="serverCertificate">TLS 服务器证书（useTls=true 时必需）</param>
    /// <param name="requireClientCertificate">是否要求客户端证书</param>
    public TcpTransportListener(
        IPEndPoint endpoint,
        bool useTls = false,
        X509Certificate2? serverCertificate = null,
        bool requireClientCertificate = false)
    {
        if (useTls && serverCertificate == null)
        {
            throw new ArgumentException("使用 TLS 时必须提供服务器证书。", nameof(serverCertificate));
        }

        _listener = new TcpListener(endpoint);
        _useTls = useTls;
        _serverCertificate = serverCertificate;
        _requireClientCertificate = requireClientCertificate;
    }

    /// <summary>
    /// 创建 TCP 传输监听器。
    /// </summary>
    /// <param name="address">监听地址</param>
    /// <param name="port">监听端口</param>
    /// <param name="useTls">是否使用 TLS</param>
    /// <param name="serverCertificate">TLS 服务器证书</param>
    /// <param name="requireClientCertificate">是否要求客户端证书</param>
    public TcpTransportListener(
        IPAddress address,
        int port,
        bool useTls = false,
        X509Certificate2? serverCertificate = null,
        bool requireClientCertificate = false)
        : this(new IPEndPoint(address, port), useTls, serverCertificate, requireClientCertificate)
    {
    }

    /// <inheritdoc/>
    public TransportType TransportType => _useTls ? TransportType.TcpTls : TransportType.Tcp;

    /// <inheritdoc/>
    public EndPoint LocalEndPoint => _listener.LocalEndpoint;

    /// <inheritdoc/>
    public bool IsListening { get; private set; }

    /// <inheritdoc/>
    public void Start()
    {
        ThrowIfDisposed();
        _listener.Start();
        IsListening = true;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (IsListening)
        {
            _listener.Stop();
            IsListening = false;
        }
    }

    /// <inheritdoc/>
    public async ValueTask<ITransportConnection> AcceptAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);

        if (_useTls)
        {
            return await TcpTransportConnection.CreateTlsAsync(
                tcpClient,
                _serverCertificate!,
                _requireClientCertificate,
                cancellationToken);
        }

        return TcpTransportConnection.Create(tcpClient);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            Stop();
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 检查是否已释放。
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TcpTransportListener));
        }
    }
}
