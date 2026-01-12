namespace System.Net.MQTT.Broker.Bridge;

/// <summary>
/// 桥接连接成功事件参数。
/// </summary>
public sealed class MqttBridgeConnectedEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置桥接名称。
    /// </summary>
    public string BridgeName { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置连接时间。
    /// </summary>
    public DateTime ConnectedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 获取或设置远程 Broker 地址。
    /// </summary>
    public string RemoteEndpoint { get; init; } = string.Empty;
}

/// <summary>
/// 桥接断开连接事件参数。
/// </summary>
public sealed class MqttBridgeDisconnectedEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置桥接名称。
    /// </summary>
    public string BridgeName { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置断开原因异常（如果有）。
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// 获取或设置是否将自动重连。
    /// </summary>
    public bool WillReconnect { get; init; }

    /// <summary>
    /// 获取或设置断开时间。
    /// </summary>
    public DateTime DisconnectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// 桥接消息转发方向。
/// </summary>
public enum BridgeDirection
{
    /// <summary>
    /// 上行（本地 -> 远程）。
    /// </summary>
    Upstream,

    /// <summary>
    /// 下行（远程 -> 本地）。
    /// </summary>
    Downstream
}

/// <summary>
/// 桥接消息转发事件参数。
/// </summary>
public sealed class MqttBridgeMessageForwardedEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置桥接名称。
    /// </summary>
    public string BridgeName { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置转发方向。
    /// </summary>
    public BridgeDirection Direction { get; init; }

    /// <summary>
    /// 获取或设置原始主题。
    /// </summary>
    public string OriginalTopic { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置转换后的主题。
    /// </summary>
    public string TransformedTopic { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置消息载荷大小（字节）。
    /// </summary>
    public int PayloadSize { get; init; }

    /// <summary>
    /// 获取或设置转发时间。
    /// </summary>
    public DateTime ForwardedAt { get; init; } = DateTime.UtcNow;
}
