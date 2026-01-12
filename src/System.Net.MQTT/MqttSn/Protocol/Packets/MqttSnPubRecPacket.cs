using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN PUBREC 报文。
/// 发布收到（QoS 2 第一步）。
///
/// 格式:
/// | Length (1) | MsgType (1) | MsgId (2) |
/// </summary>
public sealed class MqttSnPubRecPacket : IMqttSnPacket
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
    public MqttSnPacketType PacketType => MqttSnPacketType.PubRec;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.PubRec;
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
    public static MqttSnPubRecPacket Parse(ReadOnlySpan<byte> buffer)
    {
        return new MqttSnPubRecPacket
        {
            MessageId = (ushort)((buffer[2] << 8) | buffer[3])
        };
    }
}
