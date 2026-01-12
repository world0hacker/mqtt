using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// UNSUBSCRIBE 报文解析器接口。
/// </summary>
public interface IUnsubscribePacketParser : IMqttPacketParser<MqttUnsubscribePacket>
{
}
