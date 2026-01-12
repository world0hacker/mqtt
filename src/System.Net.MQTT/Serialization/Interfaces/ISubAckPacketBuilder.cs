using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// SUBACK 报文构建器接口。
/// </summary>
public interface ISubAckPacketBuilder : IMqttPacketBuilder<MqttSubAckPacket>
{
    /// <summary>
    /// 创建 SUBACK 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCodes">订阅结果码列表</param>
    /// <returns>SUBACK 报文</returns>
    MqttSubAckPacket Create(ushort packetId, IReadOnlyList<byte> reasonCodes);
}
