using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 PUBLISH 报文解析器。
/// </summary>
public sealed class V500PublishPacketParser : IPublishPacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500PublishPacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttPublishPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttPublishPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var packet = new MqttPublishPacket();
        packet.SetFromFlags(flags);

        var reader = new MqttBinaryReader(data);
        packet.Topic = reader.ReadString();

        if (packet.QoS != MqttQualityOfService.AtMostOnce)
        {
            packet.PacketId = reader.ReadUInt16();
        }

        // MQTT 5.0 属性
        var propertiesLength = (int)reader.ReadVariableByteInteger();
        if (propertiesLength > 0)
        {
            packet.Properties = _propertyParser.ParsePublishProperties(ref reader, propertiesLength);
        }

        packet.Payload = reader.ReadRemainingBytesAsMemory();
        return packet;
    }

    public MqttApplicationMessage ToApplicationMessage(MqttPublishPacket packet)
    {
        var msg = new MqttApplicationMessage
        {
            Topic = packet.Topic,
            Payload = packet.Payload,
            QualityOfService = packet.QoS,
            Retain = packet.Retain
        };

        // 复制 V5.0 属性
        if (packet.Properties != null)
        {
            msg.PayloadFormatIndicator = packet.Properties.PayloadFormatIndicator;
            msg.MessageExpiryInterval = packet.Properties.MessageExpiryInterval;
            msg.TopicAlias = packet.Properties.TopicAlias;
            msg.ResponseTopic = packet.Properties.ResponseTopic;
            msg.CorrelationData = packet.Properties.CorrelationData;
            msg.ContentType = packet.Properties.ContentType;

            foreach (var subId in packet.Properties.SubscriptionIdentifiers)
                msg.SubscriptionIdentifiers.Add(subId);
            foreach (var prop in packet.Properties.UserProperties)
                msg.UserProperties.Add(prop);
        }

        return msg;
    }
}
