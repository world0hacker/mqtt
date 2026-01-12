namespace System.Net.MQTT.MqttSn.Protocol;

/// <summary>
/// MQTT-SN 主题类型枚举。
/// </summary>
public enum MqttSnTopicType : byte
{
    /// <summary>
    /// 普通主题 ID。通过 REGISTER 报文注册获取的 ID。
    /// </summary>
    Normal = 0x00,

    /// <summary>
    /// 预定义主题 ID。双方预先约定的主题 ID。
    /// </summary>
    Predefined = 0x01,

    /// <summary>
    /// 短主题名。2 字节的短主题名，直接作为主题 ID 使用。
    /// </summary>
    ShortName = 0x02,

    /// <summary>
    /// 保留。
    /// </summary>
    Reserved = 0x03
}
