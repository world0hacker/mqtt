using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// SUBACK 报文解析器接口。
/// </summary>
public interface ISubAckPacketParser : IMqttPacketParser<MqttSubAckPacket>
{
}
