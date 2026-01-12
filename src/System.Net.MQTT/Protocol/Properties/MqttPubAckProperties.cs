namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 PUBACK/PUBREC/PUBREL/PUBCOMP 报文属性。
/// 这些确认报文共享相同的属性结构。
/// </summary>
public sealed class MqttPubAckProperties
{
    /// <summary>
    /// 原因字符串。
    /// 人类可读的状态描述。
    /// 用于诊断，不应被应用程序解析。
    /// </summary>
    public string? ReasonString { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 应用特定的键值对元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
