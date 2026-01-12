using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// CONNECT 报文解析器接口。
/// </summary>
public interface IConnectPacketParser : IMqttPacketParser<MqttConnectPacket>
{
}
