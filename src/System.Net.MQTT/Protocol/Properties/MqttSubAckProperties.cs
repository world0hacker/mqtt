namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 SUBACK 报文属性。
/// </summary>
public sealed class MqttSubAckProperties
{
    /// <summary>
    /// 原因字符串。
    /// 人类可读的状态描述。
    /// </summary>
    public string? ReasonString { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 应用特定的键值对元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
