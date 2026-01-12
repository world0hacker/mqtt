using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// PUBACK/PUBREC/PUBREL/PUBCOMP 报文构建器接口。
/// </summary>
public interface IPubAckPacketBuilder : IMqttPacketBuilder<MqttPubAckPacket>
{
    /// <summary>
    /// 创建 PUBACK 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCode">原因码（MQTT 5.0）</param>
    /// <returns>PUBACK 报文</returns>
    MqttPubAckPacket CreatePubAck(ushort packetId, byte reasonCode = 0);

    /// <summary>
    /// 创建 PUBREC 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCode">原因码（MQTT 5.0）</param>
    /// <returns>PUBREC 报文</returns>
    MqttPubAckPacket CreatePubRec(ushort packetId, byte reasonCode = 0);

    /// <summary>
    /// 创建 PUBREL 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCode">原因码（MQTT 5.0）</param>
    /// <returns>PUBREL 报文</returns>
    MqttPubAckPacket CreatePubRel(ushort packetId, byte reasonCode = 0);

    /// <summary>
    /// 创建 PUBCOMP 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCode">原因码（MQTT 5.0）</param>
    /// <returns>PUBCOMP 报文</returns>
    MqttPubAckPacket CreatePubComp(ushort packetId, byte reasonCode = 0);
}
