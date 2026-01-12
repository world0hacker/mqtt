namespace System.Net.MQTT.MqttSn.Protocol;

/// <summary>
/// MQTT-SN 返回码枚举。
/// </summary>
public enum MqttSnReturnCode : byte
{
    /// <summary>
    /// 操作成功。
    /// </summary>
    Accepted = 0x00,

    /// <summary>
    /// 拥塞。网关忙碌，客户端应稍后重试。
    /// </summary>
    Congestion = 0x01,

    /// <summary>
    /// 无效的主题 ID。
    /// </summary>
    InvalidTopicId = 0x02,

    /// <summary>
    /// 不支持的操作。
    /// </summary>
    NotSupported = 0x03
}
