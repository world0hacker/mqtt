using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN SEARCHGW 报文。
/// 客户端发送以查找可用网关。
///
/// 格式:
/// | Length (1) | MsgType (1) | Radius (1) |
/// </summary>
public sealed class MqttSnSearchGwPacket : IMqttSnPacket
{
    /// <summary>
    /// 报文固定长度。
    /// </summary>
    public const int PacketLength = 3;

    /// <summary>
    /// 获取或设置搜索半径（跳数）。
    /// 0 表示只在本地网络搜索。
    /// </summary>
    public byte Radius { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.SearchGw;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.SearchGw;
        buffer[2] = Radius;
        return PacketLength;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnSearchGwPacket Parse(ReadOnlySpan<byte> buffer)
    {
        return new MqttSnSearchGwPacket
        {
            Radius = buffer[2]
        };
    }
}
