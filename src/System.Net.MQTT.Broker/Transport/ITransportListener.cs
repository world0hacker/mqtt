using System.Net;

namespace System.Net.MQTT.Broker.Transport;

/// <summary>
/// 传输层监听器抽象接口。
/// 支持 TCP（面向连接）和 UDP（无连接）协议。
/// </summary>
public interface ITransportListener : IAsyncDisposable
{
    /// <summary>
    /// 获取传输类型。
    /// </summary>
    TransportType TransportType { get; }

    /// <summary>
    /// 获取本地端点。
    /// </summary>
    EndPoint LocalEndPoint { get; }

    /// <summary>
    /// 获取是否正在监听。
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// 开始监听。
    /// </summary>
    void Start();

    /// <summary>
    /// 停止监听。
    /// </summary>
    void Stop();

    /// <summary>
    /// 异步接受连接。
    /// 对于 TCP：等待并返回新的客户端连接。
    /// 对于 UDP：返回虚拟连接（基于远程端点识别）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>传输连接实例</returns>
    ValueTask<ITransportConnection> AcceptAsync(CancellationToken cancellationToken = default);
}
