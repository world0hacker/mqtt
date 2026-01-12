using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN WILLTOPICUPD 报文。
/// 更新遗嘱主题。
///
/// 格式:
/// | Length (1-3) | MsgType (1) | Flags (1) | WillTopic (n) |
///
/// 空的报文（长度为 2）表示删除遗嘱。
/// </summary>
public sealed class MqttSnWillTopicUpdPacket : IMqttSnPacket
{
    /// <summary>
    /// 获取或设置标志位（包含 QoS 和 Retain）。
    /// </summary>
    public MqttSnFlags Flags { get; set; }

    /// <summary>
    /// 获取或设置遗嘱主题。
    /// null 或空表示删除遗嘱。
    /// </summary>
    public string? WillTopic { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.WillTopicUpd;

    /// <inheritdoc/>
    public int Length
    {
        get
        {
            if (string.IsNullOrEmpty(WillTopic))
            {
                return 2;
            }

            var topicBytes = Encoding.UTF8.GetByteCount(WillTopic);
            var payloadLength = 1 + topicBytes;
            return payloadLength <= 253 ? 2 + payloadLength : 4 + payloadLength;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        if (string.IsNullOrEmpty(WillTopic))
        {
            buffer[0] = 2;
            buffer[1] = (byte)MqttSnPacketType.WillTopicUpd;
            return 2;
        }

        var topicBytes = Encoding.UTF8.GetBytes(WillTopic);
        var payloadLength = 1 + topicBytes.Length;
        int offset;

        if (payloadLength <= 253)
        {
            buffer[0] = (byte)(2 + payloadLength);
            buffer[1] = (byte)MqttSnPacketType.WillTopicUpd;
            offset = 2;
        }
        else
        {
            buffer[0] = 0x01;
            var totalLength = (ushort)(4 + payloadLength);
            buffer[1] = (byte)(totalLength >> 8);
            buffer[2] = (byte)totalLength;
            buffer[3] = (byte)MqttSnPacketType.WillTopicUpd;
            offset = 4;
        }

        buffer[offset++] = Flags;
        topicBytes.CopyTo(buffer.Slice(offset));

        return offset + topicBytes.Length;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="length">报文长度</param>
    /// <param name="headerLength">头部长度</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnWillTopicUpdPacket Parse(ReadOnlySpan<byte> buffer, int length, int headerLength)
    {
        var packet = new MqttSnWillTopicUpdPacket();

        if (length <= headerLength)
        {
            return packet;
        }

        var dataOffset = headerLength;
        packet.Flags = buffer[dataOffset++];

        var topicLength = length - dataOffset;
        if (topicLength > 0)
        {
            packet.WillTopic = Encoding.UTF8.GetString(buffer.Slice(dataOffset, topicLength));
        }

        return packet;
    }
}
