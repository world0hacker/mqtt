namespace System.Net.MQTT.Protocol;

/// <summary>
/// MQTT 报文基础接口。
/// 所有报文类型都应实现此接口。
/// </summary>
public interface IMqttPacket
{
    /// <summary>
    /// 获取报文类型。
    /// </summary>
    MqttPacketType PacketType { get; }
}

/// <summary>
/// 带有报文标识符的 MQTT 报文接口。
/// 用于 QoS > 0 的报文类型（PUBLISH、PUBACK、PUBREC、PUBREL、PUBCOMP、SUBSCRIBE、SUBACK、UNSUBSCRIBE、UNSUBACK）。
/// </summary>
public interface IMqttPacketWithId : IMqttPacket
{
    /// <summary>
    /// 获取或设置报文标识符。
    /// 范围：1-65535，0 为无效值。
    /// </summary>
    ushort PacketId { get; set; }
}
