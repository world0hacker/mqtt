using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// AUTH 报文解析器接口（仅 MQTT 5.0）。
/// </summary>
public interface IAuthPacketParser : IMqttPacketParser<MqttAuthPacket>
{
}
