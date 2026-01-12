using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN WILLMSG 报文。
/// 客户端发送遗嘱消息内容。
///
/// 格式:
/// | Length (1-3) | MsgType (1) | WillMsg (n) |
/// </summary>
public sealed class MqttSnWillMsgPacket : IMqttSnPacket
{
    /// <summary>
    /// 获取或设置遗嘱消息内容。
    /// </summary>
    public byte[] WillMessage { get; set; } = Array.Empty<byte>();

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.WillMsg;

    /// <inheritdoc/>
    public int Length
    {
        get
        {
            var payloadLength = WillMessage.Length;
            return payloadLength <= 253 ? 2 + payloadLength : 4 + payloadLength;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        int offset;

        if (WillMessage.Length <= 253)
        {
            buffer[0] = (byte)(2 + WillMessage.Length);
            buffer[1] = (byte)MqttSnPacketType.WillMsg;
            offset = 2;
        }
        else
        {
            buffer[0] = 0x01;
            var totalLength = (ushort)(4 + WillMessage.Length);
            buffer[1] = (byte)(totalLength >> 8);
            buffer[2] = (byte)totalLength;
            buffer[3] = (byte)MqttSnPacketType.WillMsg;
            offset = 4;
        }

        WillMessage.CopyTo(buffer.Slice(offset));
        return offset + WillMessage.Length;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="length">报文长度</param>
    /// <param name="headerLength">头部长度</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnWillMsgPacket Parse(ReadOnlySpan<byte> buffer, int length, int headerLength)
    {
        var packet = new MqttSnWillMsgPacket();
        var msgLength = length - headerLength;

        if (msgLength > 0)
        {
            packet.WillMessage = buffer.Slice(headerLength, msgLength).ToArray();
        }

        return packet;
    }
}
