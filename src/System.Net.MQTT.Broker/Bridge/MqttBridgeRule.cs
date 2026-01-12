namespace System.Net.MQTT.Broker.Bridge;

/// <summary>
/// MQTT 桥接主题同步规则。
/// </summary>
public sealed class MqttBridgeRule
{
    /// <summary>
    /// 获取或设置本地主题过滤器（支持通配符 + 和 #）。
    /// </summary>
    public string LocalTopicFilter { get; set; } = "#";

    /// <summary>
    /// 获取或设置远程主题前缀（可用于主题转换）。
    /// 例如：本地 "sensor/temp" + 远程前缀 "site1/" = 远程 "site1/sensor/temp"
    /// </summary>
    public string? RemoteTopicPrefix { get; set; }

    /// <summary>
    /// 获取或设置本地主题前缀（可用于主题转换）。
    /// 用于下行同步时，将远程主题转换为本地主题。
    /// </summary>
    public string? LocalTopicPrefix { get; set; }

    /// <summary>
    /// 获取或设置是否启用此规则。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 获取或设置此规则的 QoS 级别（可选，不设置则使用桥接默认值）。
    /// </summary>
    public MqttQualityOfService? QualityOfService { get; set; }
}
