using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN REGISTER 报文。
/// 用于注册主题名并获取主题 ID。
///
/// 格式:
/// | Length (1-3) | MsgType (1) | TopicId (2) | MsgId (2) | TopicName (n) |
/// </summary>
public sealed class MqttSnRegisterPacket : IMqttSnPacket
{
    /// <summary>
    /// 获取或设置主题 ID。
    /// 网关分配时填写，客户端请求时为 0x0000。
    /// </summary>
    public ushort TopicId { get; set; }

    /// <summary>
    /// 获取或设置消息 ID。
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// 获取或设置主题名。
    /// </summary>
    public string TopicName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.Register;

    /// <inheritdoc/>
    public int Length
    {
        get
        {
            var topicNameBytes = Encoding.UTF8.GetByteCount(TopicName);
            var payloadLength = 2 + 2 + topicNameBytes; // TopicId + MsgId + TopicName
            return payloadLength <= 253 ? 2 + payloadLength : 4 + payloadLength;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        var topicNameBytes = Encoding.UTF8.GetBytes(TopicName);
        var payloadLength = 2 + 2 + topicNameBytes.Length;
        int offset;

        if (payloadLength <= 253)
        {
            buffer[0] = (byte)(2 + payloadLength);
            buffer[1] = (byte)MqttSnPacketType.Register;
            offset = 2;
        }
        else
        {
            buffer[0] = 0x01;
            var totalLength = (ushort)(4 + payloadLength);
            buffer[1] = (byte)(totalLength >> 8);
            buffer[2] = (byte)totalLength;
            buffer[3] = (byte)MqttSnPacketType.Register;
            offset = 4;
        }

        buffer[offset++] = (byte)(TopicId >> 8);
        buffer[offset++] = (byte)TopicId;
        buffer[offset++] = (byte)(MessageId >> 8);
        buffer[offset++] = (byte)MessageId;

        topicNameBytes.CopyTo(buffer.Slice(offset));
        offset += topicNameBytes.Length;

        return offset;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="length">报文长度</param>
    /// <param name="headerLength">头部长度</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnRegisterPacket Parse(ReadOnlySpan<byte> buffer, int length, int headerLength)
    {
        var dataOffset = headerLength;

        var packet = new MqttSnRegisterPacket
        {
            TopicId = (ushort)((buffer[dataOffset] << 8) | buffer[dataOffset + 1])
        };
        dataOffset += 2;

        packet.MessageId = (ushort)((buffer[dataOffset] << 8) | buffer[dataOffset + 1]);
        dataOffset += 2;

        var topicNameLength = length - dataOffset;
        if (topicNameLength > 0)
        {
            packet.TopicName = Encoding.UTF8.GetString(buffer.Slice(dataOffset, topicNameLength));
        }

        return packet;
    }
}
