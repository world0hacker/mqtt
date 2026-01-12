namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// MQTT 集群节点接口。
/// </summary>
public interface IMqttClusterNode : IAsyncDisposable
{
    /// <summary>
    /// 获取节点唯一标识。
    /// </summary>
    string NodeId { get; }

    /// <summary>
    /// 获取节点是否正在运行。
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 获取已发现的对等节点。
    /// </summary>
    IReadOnlyCollection<ClusterPeerInfo> Peers { get; }

    /// <summary>
    /// 获取集群配置。
    /// </summary>
    MqttClusterOptions Options { get; }

    /// <summary>
    /// 当对等节点加入时触发。
    /// </summary>
    event EventHandler<ClusterPeerEventArgs>? PeerJoined;

    /// <summary>
    /// 当对等节点离开时触发。
    /// </summary>
    event EventHandler<ClusterPeerEventArgs>? PeerLeft;

    /// <summary>
    /// 当消息被转发时触发。
    /// </summary>
    event EventHandler<ClusterMessageForwardedEventArgs>? MessageForwarded;

    /// <summary>
    /// 当订阅信息同步时触发。
    /// </summary>
    event EventHandler<ClusterSubscriptionSyncEventArgs>? SubscriptionSynced;

    /// <summary>
    /// 启动集群节点。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止集群节点。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 广播消息到集群。
    /// </summary>
    /// <param name="message">MQTT 应用消息</param>
    /// <param name="sourceNodeId">消息来源节点 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BroadcastMessageAsync(MqttApplicationMessage message, string sourceNodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步订阅信息到集群。
    /// </summary>
    /// <param name="topic">主题过滤器</param>
    /// <param name="isSubscribe">是否为订阅操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SyncSubscriptionAsync(string topic, bool isSubscribe, CancellationToken cancellationToken = default);
}
