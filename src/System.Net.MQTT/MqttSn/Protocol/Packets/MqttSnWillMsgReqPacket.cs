using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN WILLMSGREQ 报文。
/// 网关请求客户端发送遗嘱消息内容。
///
/// 格式:
/// | Length (1) | MsgType (1) |
/// </summary>
public sealed class MqttSnWillMsgReqPacket : IMqttSnPacket
{
    /// <summary>
    /// 报文固定长度。
    /// </summary>
    public const int PacketLength = 2;

    /// <summary>
    /// 单例实例。
    /// </summary>
    public static readonly MqttSnWillMsgReqPacket Instance = new();

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.WillMsgReq;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.WillMsgReq;
        return PacketLength;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnWillMsgReqPacket Parse(ReadOnlySpan<byte> buffer)
    {
        return Instance;
    }
}
