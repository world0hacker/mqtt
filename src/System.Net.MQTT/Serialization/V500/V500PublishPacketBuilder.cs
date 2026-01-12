using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 PUBLISH 报文构建器。
/// </summary>
public sealed class V500PublishPacketBuilder : IPublishPacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;

    public V500PublishPacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttPublishPacket CreateFromMessage(MqttApplicationMessage message, ushort packetId, bool duplicate = false)
    {
        var packet = new MqttPublishPacket
        {
            Topic = message.Topic,
            Payload = message.Payload,
            QoS = message.QualityOfService,
            Retain = message.Retain,
            Duplicate = duplicate,
            PacketId = packetId
        };

        // 复制 V5.0 属性
        if (message.PayloadFormatIndicator.HasValue || message.MessageExpiryInterval.HasValue ||
            message.TopicAlias.HasValue || !string.IsNullOrEmpty(message.ResponseTopic) ||
            !message.CorrelationData.IsEmpty || !string.IsNullOrEmpty(message.ContentType) ||
            message.UserProperties.Count > 0)
        {
            packet.Properties = new MqttPublishProperties
            {
                PayloadFormatIndicator = message.PayloadFormatIndicator,
                MessageExpiryInterval = message.MessageExpiryInterval,
                TopicAlias = message.TopicAlias,
                ResponseTopic = message.ResponseTopic,
                CorrelationData = message.CorrelationData,
                ContentType = message.ContentType
            };

            foreach (var prop in message.UserProperties)
                packet.Properties.UserProperties.Add(prop);
        }

        return packet;
    }

    public int CalculateSize(MqttPublishPacket packet)
    {
        var size = MqttBinaryWriter.GetStringSize(packet.Topic);

        if (packet.QoS != MqttQualityOfService.AtMostOnce)
            size += 2;

        var propsSize = _propertyBuilder.CalculatePublishPropertiesSize(packet.Properties);
        size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;
        size += packet.Payload.Length;

        return size;
    }

    public int Build(MqttPublishPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);
        writer.WriteString(packet.Topic);

        if (packet.QoS != MqttQualityOfService.AtMostOnce)
            writer.WriteUInt16(packet.PacketId);

        _propertyBuilder.WritePublishProperties(ref writer, packet.Properties);
        writer.WriteBytes(packet.Payload.Span);

        return writer.Position;
    }

    public byte GetFlags(MqttPublishPacket packet) => packet.GetFlags();

    public void WriteTo(MqttPublishPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        span[0] = (byte)(((int)MqttPacketType.Publish << 4) | packet.GetFlags());
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
