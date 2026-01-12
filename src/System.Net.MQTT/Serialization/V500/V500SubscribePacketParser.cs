using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 SUBSCRIBE 报文解析器。
/// </summary>
public sealed class V500SubscribePacketParser : ISubscribePacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500SubscribePacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttSubscribePacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttSubscribePacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var packet = new MqttSubscribePacket();
        var reader = new MqttBinaryReader(data);

        packet.PacketId = reader.ReadUInt16();

        // MQTT 5.0 属性
        var propertiesLength = (int)reader.ReadVariableByteInteger();
        if (propertiesLength > 0)
        {
            packet.Properties = _propertyParser.ParseSubscribeProperties(ref reader, propertiesLength);
        }

        // 订阅列表
        while (reader.Remaining > 0)
        {
            var topicFilter = reader.ReadString();
            var options = reader.ReadByte();

            var sub = new MqttSubscriptionOptions { TopicFilter = topicFilter };
            sub.SetFromOptionsByte(options);
            packet.Subscriptions.Add(sub);
        }

        if (packet.Subscriptions.Count == 0)
        {
            throw new MqttProtocolException("SUBSCRIBE 报文必须包含至少一个订阅");
        }

        return packet;
    }
}
