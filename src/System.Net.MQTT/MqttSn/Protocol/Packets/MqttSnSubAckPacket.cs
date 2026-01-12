using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN SUBACK 报文。
/// 订阅确认。
///
/// 格式:
/// | Length (1) | MsgType (1) | Flags (1) | TopicId (2) | MsgId (2) | ReturnCode (1) |
/// </summary>
public sealed class MqttSnSubAckPacket : IMqttSnPacket
{
    /// <summary>
    /// 报文固定长度。
    /// </summary>
    public const int PacketLength = 8;

    /// <summary>
    /// 获取或设置标志位（包含授予的 QoS）。
    /// </summary>
    public MqttSnFlags Flags { get; set; }

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
    public MqttSnPacketType PacketType => MqttSnPacketType.SubAck;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.SubAck;
        buffer[2] = Flags;
        buffer[3] = (byte)(TopicId >> 8);
        buffer[4] = (byte)TopicId;
        buffer[5] = (byte)(MessageId >> 8);
        buffer[6] = (byte)MessageId;
        buffer[7] = (byte)ReturnCode;
        return PacketLength;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnSubAckPacket Parse(ReadOnlySpan<byte> buffer)
    {
        return new MqttSnSubAckPacket
        {
            Flags = buffer[2],
            TopicId = (ushort)((buffer[3] << 8) | buffer[4]),
            MessageId = (ushort)((buffer[5] << 8) | buffer[6]),
            ReturnCode = (MqttSnReturnCode)buffer[7]
        };
    }
}
