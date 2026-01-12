namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 SUBSCRIBE 报文属性。
/// </summary>
public sealed class MqttSubscribeProperties
{
    /// <summary>
    /// 订阅标识符。
    /// 用于在收到消息时标识匹配的订阅。
    /// 范围：1-268435455。
    /// </summary>
    public uint? SubscriptionIdentifier { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 应用特定的键值对元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
