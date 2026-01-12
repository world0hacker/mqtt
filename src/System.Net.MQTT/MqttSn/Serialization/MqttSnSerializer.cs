using System.Buffers;
using System.Net.MQTT.MqttSn.Protocol;
using System.Net.MQTT.MqttSn.Protocol.Packets;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.MqttSn.Serialization;

/// <summary>
/// MQTT-SN 报文序列化器。
/// 高性能的序列化和反序列化实现。
/// </summary>
public static class MqttSnSerializer
{
    /// <summary>
    /// 从数据报解析 MQTT-SN 报文。
    /// </summary>
    /// <param name="datagram">数据报缓冲区</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static IMqttSnPacket Deserialize(ReadOnlySpan<byte> datagram)
    {
        if (datagram.Length < 2)
        {
            throw new ArgumentException("数据报长度不足", nameof(datagram));
        }

        // 解析长度和消息类型
        int length;
        int headerLength;
        MqttSnPacketType packetType;

        if (datagram[0] == 0x01)
        {
            // 扩展长度格式：0x01 + 2字节长度 + 消息类型
            if (datagram.Length < 4)
            {
                throw new ArgumentException("扩展长度数据报格式不正确", nameof(datagram));
            }

            length = (datagram[1] << 8) | datagram[2];
            packetType = (MqttSnPacketType)datagram[3];
            headerLength = 4;
        }
        else
        {
            // 标准长度格式：1字节长度 + 消息类型
            length = datagram[0];
            packetType = (MqttSnPacketType)datagram[1];
            headerLength = 2;
        }

        if (datagram.Length < length)
        {
            throw new ArgumentException($"数据报长度不足: 期望 {length}，实际 {datagram.Length}", nameof(datagram));
        }

        return packetType switch
        {
            MqttSnPacketType.Advertise => MqttSnAdvertisePacket.Parse(datagram),
            MqttSnPacketType.SearchGw => MqttSnSearchGwPacket.Parse(datagram),
            MqttSnPacketType.GwInfo => MqttSnGwInfoPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.Connect => MqttSnConnectPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.ConnAck => MqttSnConnAckPacket.Parse(datagram),
            MqttSnPacketType.WillTopicReq => MqttSnWillTopicReqPacket.Parse(datagram),
            MqttSnPacketType.WillTopic => MqttSnWillTopicPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.WillMsgReq => MqttSnWillMsgReqPacket.Parse(datagram),
            MqttSnPacketType.WillMsg => MqttSnWillMsgPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.Register => MqttSnRegisterPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.RegAck => MqttSnRegAckPacket.Parse(datagram),
            MqttSnPacketType.Publish => MqttSnPublishPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.PubAck => MqttSnPubAckPacket.Parse(datagram),
            MqttSnPacketType.PubComp => MqttSnPubCompPacket.Parse(datagram),
            MqttSnPacketType.PubRec => MqttSnPubRecPacket.Parse(datagram),
            MqttSnPacketType.PubRel => MqttSnPubRelPacket.Parse(datagram),
            MqttSnPacketType.Subscribe => MqttSnSubscribePacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.SubAck => MqttSnSubAckPacket.Parse(datagram),
            MqttSnPacketType.Unsubscribe => MqttSnUnsubscribePacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.UnsubAck => MqttSnUnsubAckPacket.Parse(datagram),
            MqttSnPacketType.PingReq => MqttSnPingReqPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.PingResp => MqttSnPingRespPacket.Parse(datagram),
            MqttSnPacketType.Disconnect => MqttSnDisconnectPacket.Parse(datagram, length),
            MqttSnPacketType.WillTopicUpd => MqttSnWillTopicUpdPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.WillTopicResp => MqttSnWillTopicRespPacket.Parse(datagram),
            MqttSnPacketType.WillMsgUpd => MqttSnWillMsgUpdPacket.Parse(datagram, length, headerLength),
            MqttSnPacketType.WillMsgResp => MqttSnWillMsgRespPacket.Parse(datagram),
            _ => throw new NotSupportedException($"不支持的报文类型: {packetType}")
        };
    }

    /// <summary>
    /// 序列化 MQTT-SN 报文到缓冲区。
    /// </summary>
    /// <param name="packet">要序列化的报文</param>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>写入的字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Serialize(IMqttSnPacket packet, Span<byte> buffer)
    {
        if (buffer.Length < packet.Length)
        {
            throw new ArgumentException($"缓冲区不足: 需要 {packet.Length}，实际 {buffer.Length}", nameof(buffer));
        }

        return packet.WriteTo(buffer);
    }

    /// <summary>
    /// 序列化 MQTT-SN 报文，使用 ArrayPool 分配内存。
    /// 调用者负责归还缓冲区。
    /// </summary>
    /// <param name="packet">要序列化的报文</param>
    /// <param name="rentedBuffer">租用的缓冲区</param>
    /// <returns>实际使用的字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SerializeWithPooledBuffer(IMqttSnPacket packet, out byte[] rentedBuffer)
    {
        var length = packet.Length;
        rentedBuffer = ArrayPool<byte>.Shared.Rent(length);
        return packet.WriteTo(rentedBuffer);
    }

    /// <summary>
    /// 尝试从数据报解析报文类型。
    /// </summary>
    /// <param name="datagram">数据报缓冲区</param>
    /// <param name="packetType">解析出的报文类型</param>
    /// <returns>是否成功解析</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetPacketType(ReadOnlySpan<byte> datagram, out MqttSnPacketType packetType)
    {
        packetType = default;

        if (datagram.Length < 2)
        {
            return false;
        }

        if (datagram[0] == 0x01)
        {
            if (datagram.Length < 4)
            {
                return false;
            }
            packetType = (MqttSnPacketType)datagram[3];
        }
        else
        {
            packetType = (MqttSnPacketType)datagram[1];
        }

        return true;
    }

    /// <summary>
    /// 尝试获取数据报的完整长度。
    /// </summary>
    /// <param name="datagram">数据报缓冲区</param>
    /// <param name="length">报文长度</param>
    /// <returns>是否成功获取长度</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetLength(ReadOnlySpan<byte> datagram, out int length)
    {
        length = 0;

        if (datagram.Length < 1)
        {
            return false;
        }

        if (datagram[0] == 0x01)
        {
            if (datagram.Length < 3)
            {
                return false;
            }
            length = (datagram[1] << 8) | datagram[2];
        }
        else
        {
            length = datagram[0];
        }

        return true;
    }
}
