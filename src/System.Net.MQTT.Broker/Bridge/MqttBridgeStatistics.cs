namespace System.Net.MQTT.Broker.Bridge;

/// <summary>
/// MQTT 桥接统计信息。
/// </summary>
public sealed class MqttBridgeStatistics
{
    /// <summary>
    /// 获取或设置桥接名称。
    /// </summary>
    public string BridgeName { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置是否已连接。
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    /// 获取或设置连接时间。
    /// </summary>
    public DateTime? ConnectedAt { get; init; }

    /// <summary>
    /// 获取或设置上行消息计数（本地 -> 远程）。
    /// </summary>
    public long UpstreamMessageCount { get; init; }

    /// <summary>
    /// 获取或设置下行消息计数（远程 -> 本地）。
    /// </summary>
    public long DownstreamMessageCount { get; init; }

    /// <summary>
    /// 获取或设置上行字节计数。
    /// </summary>
    public long UpstreamByteCount { get; init; }

    /// <summary>
    /// 获取或设置下行字节计数。
    /// </summary>
    public long DownstreamByteCount { get; init; }

    /// <summary>
    /// 获取或设置重连次数。
    /// </summary>
    public int ReconnectCount { get; init; }

    /// <summary>
    /// 获取或设置最后活动时间。
    /// </summary>
    public DateTime? LastActivityAt { get; init; }

    /// <summary>
    /// 获取或设置上行失败消息计数。
    /// </summary>
    public long UpstreamFailedCount { get; init; }

    /// <summary>
    /// 获取或设置下行失败消息计数。
    /// </summary>
    public long DownstreamFailedCount { get; init; }

    /// <summary>
    /// 获取连接持续时间。
    /// </summary>
    public TimeSpan? ConnectionDuration => ConnectedAt.HasValue && IsConnected
        ? DateTime.UtcNow - ConnectedAt.Value
        : null;
}
