namespace System.Net.MQTT;

/// <summary>
/// MQTT 消息传递的服务质量级别。
/// </summary>
public enum MqttQualityOfService : byte
{
    /// <summary>
    /// 最多一次传递。发送即忘，不保证到达。
    /// </summary>
    AtMostOnce = 0,

    /// <summary>
    /// 至少一次传递。确认式传递，可能重复。
    /// </summary>
    AtLeastOnce = 1,

    /// <summary>
    /// 恰好一次传递。保证式传递，不重复。
    /// </summary>
    ExactlyOnce = 2
}
