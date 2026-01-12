using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// SUBSCRIBE 报文构建器接口。
/// </summary>
public interface ISubscribePacketBuilder : IMqttPacketBuilder<MqttSubscribePacket>
{
    /// <summary>
    /// 创建 SUBSCRIBE 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="subscriptions">订阅列表</param>
    /// <returns>SUBSCRIBE 报文</returns>
    MqttSubscribePacket Create(ushort packetId, IReadOnlyList<MqttTopicSubscription> subscriptions);
}
