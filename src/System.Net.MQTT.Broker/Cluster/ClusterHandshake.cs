namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// 集群握手数据。
/// </summary>
public sealed class ClusterHandshake
{
    /// <summary>
    /// 获取或设置协议版本。
    /// </summary>
    public byte ProtocolVersion { get; set; } = 1;

    /// <summary>
    /// 获取或设置节点 ID。
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置集群名称。
    /// </summary>
    public string ClusterName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置监听端口。
    /// </summary>
    public int ListenPort { get; set; }

    /// <summary>
    /// 获取或设置节点地址（用于其他节点连接）。
    /// </summary>
    public string? NodeAddress { get; set; }

    /// <summary>
    /// 获取或设置握手时间戳。
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// 创建握手请求。
    /// </summary>
    /// <param name="nodeId">节点 ID</param>
    /// <param name="clusterName">集群名称</param>
    /// <param name="listenPort">监听端口</param>
    /// <returns>握手数据</returns>
    public static ClusterHandshake Create(string nodeId, string clusterName, int listenPort)
    {
        return new ClusterHandshake
        {
            NodeId = nodeId,
            ClusterName = clusterName,
            ListenPort = listenPort,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}
