namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// 集群对等节点信息。
/// </summary>
public sealed class ClusterPeerInfo
{
    /// <summary>
    /// 获取或设置节点 ID。
    /// </summary>
    public string NodeId { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置节点地址（IP:Port）。
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置监听端口。
    /// </summary>
    public int ListenPort { get; init; }

    /// <summary>
    /// 获取或设置加入时间。
    /// </summary>
    public DateTime JoinedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 获取或设置最后心跳时间。
    /// </summary>
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 获取或设置是否为入站连接（被动接受的连接）。
    /// </summary>
    public bool IsInbound { get; init; }

    /// <summary>
    /// 获取节点在线时长。
    /// </summary>
    public TimeSpan Uptime => DateTime.UtcNow - JoinedAt;

    /// <summary>
    /// 获取距离上次心跳的时间。
    /// </summary>
    public TimeSpan TimeSinceLastHeartbeat => DateTime.UtcNow - LastHeartbeat;
}
