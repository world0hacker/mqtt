using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// PUBLISH 报文构建器接口。
/// </summary>
public interface IPublishPacketBuilder : IMqttPacketBuilder<MqttPublishPacket>
{
    /// <summary>
    /// 从应用消息创建 PUBLISH 报文。
    /// </summary>
    /// <param name="message">应用消息</param>
    /// <param name="packetId">报文标识符（QoS > 0 时必须）</param>
    /// <param name="duplicate">是否为重复发送</param>
    /// <returns>PUBLISH 报文</returns>
    MqttPublishPacket CreateFromMessage(MqttApplicationMessage message, ushort packetId, bool duplicate = false);
}
