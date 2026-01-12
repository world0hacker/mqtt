namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// MQTT 集群配置选项。
/// </summary>
public sealed class MqttClusterOptions
{
    /// <summary>
    /// 获取或设置本节点 ID（自动生成或手动指定）。
    /// </summary>
    public string NodeId { get; set; } = $"node-{Guid.NewGuid().ToString("N")[..8]}";

    /// <summary>
    /// 获取或设置集群名称（用于隔离不同集群）。
    /// </summary>
    public string ClusterName { get; set; } = "mqtt-cluster";

    /// <summary>
    /// 获取或设置集群通信端口。
    /// </summary>
    public int ClusterPort { get; set; } = 11883;

    /// <summary>
    /// 获取或设置绑定地址。
    /// </summary>
    public string BindAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// 获取或设置种子节点列表（初始发现用）。
    /// 格式为 "host:port" 或 "host"（使用默认端口）。
    /// </summary>
    public List<string> SeedNodes { get; set; } = new();

    /// <summary>
    /// 获取或设置心跳间隔（毫秒）。
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 获取或设置节点超时时间（毫秒）。
    /// 超过此时间没有收到心跳的节点将被视为离线。
    /// </summary>
    public int NodeTimeoutMs { get; set; } = 15000;

    /// <summary>
    /// 获取或设置是否启用消息去重。
    /// </summary>
    public bool EnableDeduplication { get; set; } = true;

    /// <summary>
    /// 获取或设置消息 ID 缓存过期时间（秒）。
    /// </summary>
    public int MessageIdCacheExpirySeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置连接重试延迟（毫秒）。
    /// </summary>
    public int ReconnectDelayMs { get; set; } = 5000;

    /// <summary>
    /// 获取或设置最大重试次数（0 表示无限重试）。
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 0;

    /// <summary>
    /// 获取或设置接收缓冲区大小。
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 8192;

    /// <summary>
    /// 获取或设置发送缓冲区大小。
    /// </summary>
    public int SendBufferSize { get; set; } = 8192;
}
