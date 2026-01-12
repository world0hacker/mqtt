using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// PUBACK/PUBREC/PUBREL/PUBCOMP 报文解析器接口。
/// 这些报文结构相同，由解析器根据报文类型区分。
/// </summary>
public interface IPubAckPacketParser : IMqttPacketParser<MqttPubAckPacket>
{
}
