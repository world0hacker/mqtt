using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN UNSUBSCRIBE 报文。
/// 取消订阅主题。
///
/// 格式:
/// | Length (1-3) | MsgType (1) | Flags (1) | MsgId (2) | TopicIdOrName (2 or n) |
/// </summary>
public sealed class MqttSnUnsubscribePacket : IMqttSnPacket
{
    /// <summary>
    /// 获取或设置标志位。
    /// </summary>
    public MqttSnFlags Flags { get; set; }

    /// <summary>
    /// 获取或设置消息 ID。
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// 获取或设置主题 ID（当 TopicType 为 Predefined 或 ShortName 时使用）。
    /// </summary>
    public ushort TopicId { get; set; }

    /// <summary>
    /// 获取或设置主题名（当 TopicType 为 Normal 时使用）。
    /// </summary>
    public string? TopicName { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.Unsubscribe;

    /// <inheritdoc/>
    public int Length
    {
        get
        {
            int topicLength;
            if (Flags.TopicType == MqttSnTopicType.Normal)
            {
                topicLength = TopicName != null ? Encoding.UTF8.GetByteCount(TopicName) : 0;
            }
            else
            {
                topicLength = 2;
            }

            var payloadLength = 1 + 2 + topicLength;
            return payloadLength <= 253 ? 2 + payloadLength : 4 + payloadLength;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        byte[] topicBytes;
        if (Flags.TopicType == MqttSnTopicType.Normal)
        {
            topicBytes = TopicName != null ? Encoding.UTF8.GetBytes(TopicName) : Array.Empty<byte>();
        }
        else
        {
            topicBytes = new byte[] { (byte)(TopicId >> 8), (byte)TopicId };
        }

        var payloadLength = 1 + 2 + topicBytes.Length;
        int offset;

        if (payloadLength <= 253)
        {
            buffer[0] = (byte)(2 + payloadLength);
            buffer[1] = (byte)MqttSnPacketType.Unsubscribe;
            offset = 2;
        }
        else
        {
            buffer[0] = 0x01;
            var totalLength = (ushort)(4 + payloadLength);
            buffer[1] = (byte)(totalLength >> 8);
            buffer[2] = (byte)totalLength;
            buffer[3] = (byte)MqttSnPacketType.Unsubscribe;
            offset = 4;
        }

        buffer[offset++] = Flags;
        buffer[offset++] = (byte)(MessageId >> 8);
        buffer[offset++] = (byte)MessageId;

        topicBytes.CopyTo(buffer.Slice(offset));
        offset += topicBytes.Length;

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
    public static MqttSnUnsubscribePacket Parse(ReadOnlySpan<byte> buffer, int length, int headerLength)
    {
        var dataOffset = headerLength;

        var packet = new MqttSnUnsubscribePacket
        {
            Flags = buffer[dataOffset++]
        };

        packet.MessageId = (ushort)((buffer[dataOffset] << 8) | buffer[dataOffset + 1]);
        dataOffset += 2;

        var topicLength = length - dataOffset;
        if (packet.Flags.TopicType == MqttSnTopicType.Normal)
        {
            if (topicLength > 0)
            {
                packet.TopicName = Encoding.UTF8.GetString(buffer.Slice(dataOffset, topicLength));
            }
        }
        else
        {
            if (topicLength >= 2)
            {
                packet.TopicId = (ushort)((buffer[dataOffset] << 8) | buffer[dataOffset + 1]);
            }
        }

        return packet;
    }
}
