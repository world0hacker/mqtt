using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN DISCONNECT 报文。
/// 断开连接或请求进入睡眠状态。
///
/// 格式:
/// | Length (1) | MsgType (1) | Duration (0 or 2) |
///
/// 当包含 Duration 时，表示客户端请求进入睡眠状态。
/// Duration 指定睡眠时间（秒）。
/// </summary>
public sealed class MqttSnDisconnectPacket : IMqttSnPacket
{
    /// <summary>
    /// 获取或设置睡眠时长（秒）。
    /// null 表示完全断开连接，非 null 表示进入睡眠状态。
    /// </summary>
    public ushort? Duration { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.Disconnect;

    /// <inheritdoc/>
    public int Length => Duration.HasValue ? 4 : 2;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        if (Duration.HasValue)
        {
            buffer[0] = 4;
            buffer[1] = (byte)MqttSnPacketType.Disconnect;
            buffer[2] = (byte)(Duration.Value >> 8);
            buffer[3] = (byte)Duration.Value;
            return 4;
        }

        buffer[0] = 2;
        buffer[1] = (byte)MqttSnPacketType.Disconnect;
        return 2;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="length">报文长度</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnDisconnectPacket Parse(ReadOnlySpan<byte> buffer, int length)
    {
        var packet = new MqttSnDisconnectPacket();

        if (length >= 4)
        {
            packet.Duration = (ushort)((buffer[2] << 8) | buffer[3]);
        }

        return packet;
    }
}
