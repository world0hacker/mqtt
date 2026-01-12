using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// UNSUBACK 报文构建器接口。
/// </summary>
public interface IUnsubAckPacketBuilder : IMqttPacketBuilder<MqttUnsubAckPacket>
{
    /// <summary>
    /// 创建 UNSUBACK 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCodes">原因码列表（MQTT 5.0）。MQTT 3.1.1 传 null。</param>
    /// <returns>UNSUBACK 报文</returns>
    MqttUnsubAckPacket Create(ushort packetId, IReadOnlyList<byte>? reasonCodes = null);
}
