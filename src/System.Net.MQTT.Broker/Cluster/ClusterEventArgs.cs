namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// 集群对等节点事件参数。
/// </summary>
public sealed class ClusterPeerEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置对等节点信息。
    /// </summary>
    public ClusterPeerInfo Peer { get; init; } = null!;
}

/// <summary>
/// 集群消息转发事件参数。
/// </summary>
public sealed class ClusterMessageForwardedEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置来源节点 ID。
    /// </summary>
    public string SourceNodeId { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置消息主题。
    /// </summary>
    public string Topic { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置消息载荷大小。
    /// </summary>
    public int PayloadSize { get; init; }

    /// <summary>
    /// 获取或设置转发到的节点数量。
    /// </summary>
    public int ForwardedToCount { get; init; }
}

/// <summary>
/// 集群订阅同步事件参数。
/// </summary>
public sealed class ClusterSubscriptionSyncEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置来源节点 ID。
    /// </summary>
    public string SourceNodeId { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置主题过滤器。
    /// </summary>
    public string TopicFilter { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置是否为订阅操作（false 表示取消订阅）。
    /// </summary>
    public bool IsSubscribe { get; init; }
}

/// <summary>
/// 集群保留消息同步事件参数。
/// </summary>
public sealed class ClusterRetainedSyncEventArgs : EventArgs
{
    /// <summary>
    /// 获取或设置来源节点 ID。
    /// </summary>
    public string SourceNodeId { get; init; } = string.Empty;

    /// <summary>
    /// 获取或设置同步的保留消息数量。
    /// </summary>
    public int MessageCount { get; init; }

    /// <summary>
    /// 获取或设置是否为请求（false 表示数据响应）。
    /// </summary>
    public bool IsRequest { get; init; }
}
