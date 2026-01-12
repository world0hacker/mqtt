using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// PUBLISH 报文解析器接口。
/// </summary>
public interface IPublishPacketParser : IMqttPacketParser<MqttPublishPacket>
{
    /// <summary>
    /// 将 PUBLISH 报文转换为应用消息。
    /// </summary>
    /// <param name="packet">PUBLISH 报文</param>
    /// <returns>应用消息</returns>
    MqttApplicationMessage ToApplicationMessage(MqttPublishPacket packet);
}
