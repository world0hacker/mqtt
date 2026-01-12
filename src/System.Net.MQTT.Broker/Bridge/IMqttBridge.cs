namespace System.Net.MQTT.Broker.Bridge;

/// <summary>
/// MQTT 桥接接口。
/// 支持子 Broker 连接到父 Broker，实现消息双向同步。
/// </summary>
public interface IMqttBridge : IAsyncDisposable
{
    /// <summary>
    /// 获取桥接名称/标识。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 获取桥接是否已连接。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 获取桥接配置。
    /// </summary>
    MqttBridgeOptions Options { get; }

    /// <summary>
    /// 当桥接连接成功时触发。
    /// </summary>
    event EventHandler<MqttBridgeConnectedEventArgs>? Connected;

    /// <summary>
    /// 当桥接断开连接时触发。
    /// </summary>
    event EventHandler<MqttBridgeDisconnectedEventArgs>? Disconnected;

    /// <summary>
    /// 当桥接消息转发时触发。
    /// </summary>
    event EventHandler<MqttBridgeMessageForwardedEventArgs>? MessageForwarded;

    /// <summary>
    /// 启动桥接连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止桥接连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取桥接统计信息。
    /// </summary>
    /// <returns>统计信息</returns>
    MqttBridgeStatistics GetStatistics();
}
