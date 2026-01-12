using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN PUBACK 报文。
/// 发布确认（QoS 1）。
///
/// 格式:
/// | Length (1) | MsgType (1) | TopicId (2) | MsgId (2) | ReturnCode (1) |
/// </summary>
public sealed class MqttSnPubAckPacket : IMqttSnPacket
{
    /// <summary>
    /// 报文固定长度。
    /// </summary>
    public const int PacketLength = 7;

    /// <summary>
    /// 获取或设置主题 ID。
    /// </summary>
    public ushort TopicId { get; set; }

    /// <summary>
    /// 获取或设置消息 ID。
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// 获取或设置返回码。
    /// </summary>
    public MqttSnReturnCode ReturnCode { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.PubAck;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.PubAck;
        buffer[2] = (byte)(TopicId >> 8);
        buffer[3] = (byte)TopicId;
        buffer[4] = (byte)(MessageId >> 8);
        buffer[5] = (byte)MessageId;
        buffer[6] = (byte)ReturnCode;
        return PacketLength;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnPubAckPacket Parse(ReadOnlySpan<byte> buffer)
    {
        return new MqttSnPubAckPacket
        {
            TopicId = (ushort)((buffer[2] << 8) | buffer[3]),
            MessageId = (ushort)((buffer[4] << 8) | buffer[5]),
            ReturnCode = (MqttSnReturnCode)buffer[6]
        };
    }
}
