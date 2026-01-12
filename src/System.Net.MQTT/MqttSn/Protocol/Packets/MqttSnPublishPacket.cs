using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN PUBLISH 报文。
/// 发布消息到主题。
///
/// 格式:
/// | Length (1-3) | MsgType (1) | Flags (1) | TopicId (2) | MsgId (2) | Data (n) |
///
/// 注意: TopicId 的含义取决于 Flags 中的 TopicType:
/// - Normal (0x00): 通过 REGISTER 获取的主题 ID
/// - Predefined (0x01): 预定义主题 ID
/// - ShortName (0x02): 2 字节短主题名
/// </summary>
public sealed class MqttSnPublishPacket : IMqttSnPacket
{
    /// <summary>
    /// 获取或设置标志位。
    /// </summary>
    public MqttSnFlags Flags { get; set; }

    /// <summary>
    /// 获取或设置主题 ID 或短主题名。
    /// </summary>
    public ushort TopicId { get; set; }

    /// <summary>
    /// 获取或设置消息 ID。
    /// QoS 0 时应为 0x0000。
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// 获取或设置消息数据。
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.Publish;

    /// <inheritdoc/>
    public int Length
    {
        get
        {
            var payloadLength = 1 + 2 + 2 + Data.Length; // Flags + TopicId + MsgId + Data
            return payloadLength <= 253 ? 2 + payloadLength : 4 + payloadLength;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        var payloadLength = 1 + 2 + 2 + Data.Length;
        int offset;

        if (payloadLength <= 253)
        {
            buffer[0] = (byte)(2 + payloadLength);
            buffer[1] = (byte)MqttSnPacketType.Publish;
            offset = 2;
        }
        else
        {
            buffer[0] = 0x01;
            var totalLength = (ushort)(4 + payloadLength);
            buffer[1] = (byte)(totalLength >> 8);
            buffer[2] = (byte)totalLength;
            buffer[3] = (byte)MqttSnPacketType.Publish;
            offset = 4;
        }

        buffer[offset++] = Flags;
        buffer[offset++] = (byte)(TopicId >> 8);
        buffer[offset++] = (byte)TopicId;
        buffer[offset++] = (byte)(MessageId >> 8);
        buffer[offset++] = (byte)MessageId;

        Data.CopyTo(buffer.Slice(offset));
        offset += Data.Length;

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
    public static MqttSnPublishPacket Parse(ReadOnlySpan<byte> buffer, int length, int headerLength)
    {
        var dataOffset = headerLength;

        var packet = new MqttSnPublishPacket
        {
            Flags = buffer[dataOffset++]
        };

        packet.TopicId = (ushort)((buffer[dataOffset] << 8) | buffer[dataOffset + 1]);
        dataOffset += 2;

        packet.MessageId = (ushort)((buffer[dataOffset] << 8) | buffer[dataOffset + 1]);
        dataOffset += 2;

        var dataLength = length - dataOffset;
        if (dataLength > 0)
        {
            packet.Data = buffer.Slice(dataOffset, dataLength).ToArray();
        }

        return packet;
    }

    /// <summary>
    /// 获取短主题名（当 TopicType 为 ShortName 时）。
    /// </summary>
    /// <returns>2 字节短主题名字符串</returns>
    public string GetShortTopicName()
    {
        return new string(new[]
        {
            (char)(TopicId >> 8),
            (char)(TopicId & 0xFF)
        });
    }

    /// <summary>
    /// 设置短主题名。
    /// </summary>
    /// <param name="shortName">2 字符短主题名</param>
    public void SetShortTopicName(string shortName)
    {
        if (shortName.Length != 2)
            throw new ArgumentException("短主题名必须为 2 个字符", nameof(shortName));

        TopicId = (ushort)((shortName[0] << 8) | shortName[1]);
    }
}
