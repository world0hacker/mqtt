namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 UNSUBSCRIBE 报文属性。
/// </summary>
public sealed class MqttUnsubscribeProperties
{
    /// <summary>
    /// 用户属性列表。
    /// 应用特定的键值对元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
