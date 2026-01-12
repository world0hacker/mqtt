using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN PUBCOMP 报文。
/// 发布完成（QoS 2 第三步）。
///
/// 格式:
/// | Length (1) | MsgType (1) | MsgId (2) |
/// </summary>
public sealed class MqttSnPubCompPacket : IMqttSnPacket
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
    public MqttSnPacketType PacketType => MqttSnPacketType.PubComp;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.PubComp;
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
    public static MqttSnPubCompPacket Parse(ReadOnlySpan<byte> buffer)
    {
        return new MqttSnPubCompPacket
        {
            MessageId = (ushort)((buffer[2] << 8) | buffer[3])
        };
    }
}
