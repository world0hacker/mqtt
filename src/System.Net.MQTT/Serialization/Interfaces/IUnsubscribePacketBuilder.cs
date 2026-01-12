using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// UNSUBSCRIBE 报文构建器接口。
/// </summary>
public interface IUnsubscribePacketBuilder : IMqttPacketBuilder<MqttUnsubscribePacket>
{
    /// <summary>
    /// 创建 UNSUBSCRIBE 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="topicFilters">要取消订阅的主题过滤器列表</param>
    /// <returns>UNSUBSCRIBE 报文</returns>
    MqttUnsubscribePacket Create(ushort packetId, IReadOnlyList<string> topicFilters);
}
