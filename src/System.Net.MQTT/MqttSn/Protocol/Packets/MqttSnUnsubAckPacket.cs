using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN UNSUBACK 报文。
/// 取消订阅确认。
///
/// 格式:
/// | Length (1) | MsgType (1) | MsgId (2) |
/// </summary>
public sealed class MqttSnUnsubAckPacket : IMqttSnPacket
{
    /// <summary>
    /// 报文固定长度。
    /// </summary>
    public const int PacketLength = 4;

    /// <summary>
    /// 获取或设置消息 ID。
    /// </summary>
    public ushort MessageId { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.UnsubAck;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.UnsubAck;
        buffer[2] = (byte)(MessageId >> 8);
        buffer[3] = (byte)MessageId;
        return PacketLength;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnUnsubAckPacket Parse(ReadOnlySpan<byte> buffer)
    {
        return new MqttSnUnsubAckPacket
        {
            MessageId = (ushort)((buffer[2] << 8) | buffer[3])
        };
    }
}
