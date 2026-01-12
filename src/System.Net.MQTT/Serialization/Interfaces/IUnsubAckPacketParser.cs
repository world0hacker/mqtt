using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// UNSUBACK 报文解析器接口。
/// </summary>
public interface IUnsubAckPacketParser : IMqttPacketParser<MqttUnsubAckPacket>
{
}
