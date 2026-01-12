using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 SUBSCRIBE 报文解析器。
/// </summary>
public sealed class V311SubscribePacketParser : ISubscribePacketParser
{
    /// <inheritdoc/>
    public MqttSubscribePacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment)
        {
            return Parse(data.FirstSpan, flags);
        }

        var array = data.ToArray();
        return Parse(array.AsSpan(), flags);
    }

    /// <inheritdoc/>
    public MqttSubscribePacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var packet = new MqttSubscribePacket();
        var reader = new MqttBinaryReader(data);

        // 报文标识符
        packet.PacketId = reader.ReadUInt16();

        // 订阅列表
        while (reader.Remaining > 0)
        {
            var topicFilter = reader.ReadString();
            var qos = (MqttQualityOfService)(reader.ReadByte() & 0x03);

            packet.Subscriptions.Add(new MqttSubscriptionOptions
            {
                TopicFilter = topicFilter,
                QoS = qos
            });
        }

        if (packet.Subscriptions.Count == 0)
        {
            throw new MqttProtocolException("SUBSCRIBE 报文必须包含至少一个订阅");
        }

        return packet;
    }
}
