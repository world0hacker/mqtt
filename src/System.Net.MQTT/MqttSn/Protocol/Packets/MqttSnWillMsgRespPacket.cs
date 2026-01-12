using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN WILLMSGRESP 报文。
/// 遗嘱消息更新响应。
///
/// 格式:
/// | Length (1) | MsgType (1) | ReturnCode (1) |
/// </summary>
public sealed class MqttSnWillMsgRespPacket : IMqttSnPacket
{
    /// <summary>
    /// 报文固定长度。
    /// </summary>
    public const int PacketLength = 3;

    /// <summary>
    /// 获取或设置返回码。
    /// </summary>
    public MqttSnReturnCode ReturnCode { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.WillMsgResp;

    /// <inheritdoc/>
    public int Length => PacketLength;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        buffer[0] = PacketLength;
        buffer[1] = (byte)MqttSnPacketType.WillMsgResp;
        buffer[2] = (byte)ReturnCode;
        return PacketLength;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnWillMsgRespPacket Parse(ReadOnlySpan<byte> buffer)
    {
        return new MqttSnWillMsgRespPacket
        {
            ReturnCode = (MqttSnReturnCode)buffer[2]
        };
    }
}
