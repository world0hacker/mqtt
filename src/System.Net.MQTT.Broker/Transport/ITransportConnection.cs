using System.Net;

namespace System.Net.MQTT.Broker.Transport;

/// <summary>
/// 传输连接抽象接口。
/// 提供统一的读写操作，支持 TCP 流和 UDP 数据报。
/// </summary>
public interface ITransportConnection : IAsyncDisposable
{
    /// <summary>
    /// 获取连接唯一标识符。
    /// 对于 TCP：基于连接实例生成。
    /// 对于 UDP：基于远程端点 (IP:Port) 生成。
    /// </summary>
    string ConnectionId { get; }

    /// <summary>
    /// 获取传输类型。
    /// </summary>
    TransportType TransportType { get; }

    /// <summary>
    /// 获取远程端点。
    /// </summary>
    EndPoint? RemoteEndPoint { get; }

    /// <summary>
    /// 获取本地端点。
    /// </summary>
    EndPoint? LocalEndPoint { get; }

    /// <summary>
    /// 获取连接是否打开。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 获取是否支持流式读取。
    /// TCP 支持流式读取（可以读取部分数据）。
    /// UDP 不支持（必须读取完整数据报）。
    /// </summary>
    bool SupportsStreaming { get; }

    /// <summary>
    /// 异步读取数据（流式读取，用于 TCP）。
    /// 读取的数据量可能小于缓冲区大小。
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实际读取的字节数，0 表示连接已关闭</returns>
    ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步读取完整的数据报（用于 UDP）。
    /// 返回完整的 UDP 数据报内容。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>完整的数据报内容</returns>
    ValueTask<ReadOnlyMemory<byte>> ReadDatagramAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步写入数据。
    /// 对于 TCP：写入到流中。
    /// 对于 UDP：发送数据报。
    /// </summary>
    /// <param name="buffer">要发送的数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新输出缓冲区。
    /// 对于 TCP：刷新底层流。
    /// 对于 UDP：无操作（UDP 无缓冲）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 关闭连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask CloseAsync(CancellationToken cancellationToken = default);
}
