using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN ADVERTISE 报文。
/// 网关定期广播此报文以宣告自己的存在。
///
/// 格式:
/// | Length (1) | MsgType (1) | GwId (1) | Duration (2) |
/// </summary>
public sealed class MqttSnAdvertisePacket : IMqttSnPacket
{
    /// <summary>
    /// 报文固定长度。
    /// </summary>
    public const int PacketLength = 5;

    /// <summary>
    /// 获取或设置网关 ID。
    /// </summary>
    public byte GatewayId { get; set; }

    /// <summary>
    /// 获取或设置广播间隔（秒）。
    /// 客户端应在此时间内期待下一个 ADVERTISE 报文。
    /// </summary>
    public ushort Duration { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.Advertise;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.Advertise;
        buffer[2] = GatewayId;
        buffer[3] = (byte)(Duration >> 8);
        buffer[4] = (byte)Duration;
        return PacketLength;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnAdvertisePacket Parse(ReadOnlySpan<byte> buffer)
    {
        return new MqttSnAdvertisePacket
        {
            GatewayId = buffer[2],
            Duration = (ushort)((buffer[3] << 8) | buffer[4])
        };
    }
}
