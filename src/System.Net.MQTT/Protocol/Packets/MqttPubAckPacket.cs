using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT PUBACK/PUBREC/PUBREL/PUBCOMP 报文。
/// 这些报文结构相同，用于 QoS 1 和 QoS 2 的消息确认流程。
/// 通过 PacketType 属性区分具体类型。
/// </summary>
public sealed class MqttPubAckPacket : IMqttPacketWithId
{
    private MqttPacketType _packetType = MqttPacketType.PubAck;

    /// <inheritdoc/>
    public MqttPacketType PacketType
    {
        get => _packetType;
        set
        {
            if (value != MqttPacketType.PubAck &&
                value != MqttPacketType.PubRec &&
                value != MqttPacketType.PubRel &&
                value != MqttPacketType.PubComp)
            {
                throw new ArgumentException("无效的报文类型，必须是 PubAck、PubRec、PubRel 或 PubComp", nameof(value));
            }
            _packetType = value;
        }
    }

    /// <inheritdoc/>
    public ushort PacketId { get; set; }

    /// <summary>
    /// 原因码（MQTT 5.0）。
    /// 0 表示成功。
    /// </summary>
    public byte ReasonCode { get; set; }

    /// <summary>
    /// MQTT 5.0 属性。
    /// </summary>
    public MqttPubAckProperties? Properties { get; set; }

    /// <summary>
    /// 判断是否成功。
    /// </summary>
    public bool IsSuccess => ReasonCode < 0x80;

    /// <summary>
    /// 创建 PUBACK 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCode">原因码</param>
    /// <returns>PUBACK 报文</returns>
    public static MqttPubAckPacket CreatePubAck(ushort packetId, byte reasonCode = 0)
    {
        return new MqttPubAckPacket
        {
            PacketType = MqttPacketType.PubAck,
            PacketId = packetId,
            ReasonCode = reasonCode
        };
    }

    /// <summary>
    /// 创建 PUBREC 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCode">原因码</param>
    /// <returns>PUBREC 报文</returns>
    public static MqttPubAckPacket CreatePubRec(ushort packetId, byte reasonCode = 0)
    {
        return new MqttPubAckPacket
        {
            PacketType = MqttPacketType.PubRec,
            PacketId = packetId,
            ReasonCode = reasonCode
        };
    }

    /// <summary>
    /// 创建 PUBREL 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCode">原因码</param>
    /// <returns>PUBREL 报文</returns>
    public static MqttPubAckPacket CreatePubRel(ushort packetId, byte reasonCode = 0)
    {
        return new MqttPubAckPacket
        {
            PacketType = MqttPacketType.PubRel,
            PacketId = packetId,
            ReasonCode = reasonCode
        };
    }

    /// <summary>
    /// 创建 PUBCOMP 报文。
    /// </summary>
    /// <param name="packetId">报文标识符</param>
    /// <param name="reasonCode">原因码</param>
    /// <returns>PUBCOMP 报文</returns>
    public static MqttPubAckPacket CreatePubComp(ushort packetId, byte reasonCode = 0)
    {
        return new MqttPubAckPacket
        {
            PacketType = MqttPacketType.PubComp,
            PacketId = packetId,
            ReasonCode = reasonCode
        };
    }
}
