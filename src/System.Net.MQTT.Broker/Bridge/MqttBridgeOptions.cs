namespace System.Net.MQTT.Broker.Bridge;

/// <summary>
/// MQTT 桥接配置选项。
/// </summary>
public sealed class MqttBridgeOptions
{
    /// <summary>
    /// 获取或设置桥接名称（用于日志和标识）。
    /// </summary>
    public string Name { get; set; } = "bridge-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// 获取或设置远程 Broker 地址。
    /// </summary>
    public string RemoteHost { get; set; } = "localhost";

    /// <summary>
    /// 获取或设置远程 Broker 端口。
    /// </summary>
    public int RemotePort { get; set; } = 1883;

    /// <summary>
    /// 获取或设置桥接客户端 ID。
    /// </summary>
    public string ClientId { get; set; } = $"bridge-{Guid.NewGuid():N}";

    /// <summary>
    /// 获取或设置认证用户名。
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 获取或设置认证密码。
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 获取或设置是否使用 TLS。
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// 获取或设置 MQTT 协议版本。
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

    /// <summary>
    /// 获取或设置保活间隔（秒）。
    /// </summary>
    public ushort KeepAliveSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置重连延迟（毫秒）。
    /// </summary>
    public int ReconnectDelayMs { get; set; } = 5000;

    /// <summary>
    /// 获取或设置上行同步规则（本地 -> 远程）。
    /// </summary>
    public List<MqttBridgeRule> UpstreamRules { get; set; } = new();

    /// <summary>
    /// 获取或设置下行同步规则（远程 -> 本地）。
    /// </summary>
    public List<MqttBridgeRule> DownstreamRules { get; set; } = new();

    /// <summary>
    /// 获取或设置桥接消息的 QoS 级别。
    /// </summary>
    public MqttQualityOfService QualityOfService { get; set; } = MqttQualityOfService.AtLeastOnce;

    /// <summary>
    /// 获取或设置是否同步保留消息标志。
    /// </summary>
    public bool SyncRetainFlag { get; set; } = true;

    /// <summary>
    /// 获取或设置是否在连接/重连时同步保留消息。
    /// 启用后，会在连接成功时将本地保留消息（匹配上行规则）同步到远程 Broker。
    /// </summary>
    public bool SyncRetainedMessages { get; set; } = true;

    /// <summary>
    /// 获取或设置连接超时时间（秒）。
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}
