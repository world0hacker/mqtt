using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 SUBSCRIBE 报文构建器。
/// </summary>
public sealed class V500SubscribePacketBuilder : ISubscribePacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;

    public V500SubscribePacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttSubscribePacket Create(ushort packetId, IReadOnlyList<MqttTopicSubscription> subscriptions)
    {
        var packet = new MqttSubscribePacket { PacketId = packetId };

        foreach (var sub in subscriptions)
        {
            packet.Subscriptions.Add(new MqttSubscriptionOptions
            {
                TopicFilter = sub.Topic,
                QoS = sub.QualityOfService
            });
        }

        return packet;
    }

    public int CalculateSize(MqttSubscribePacket packet)
    {
        var size = 2; // PacketId

        // 属性
        var propsSize = 0;
        if (packet.Properties?.SubscriptionIdentifier != null)
            propsSize += 1 + MqttBinaryWriter.GetVariableByteIntegerSize(packet.Properties.SubscriptionIdentifier.Value);
        if (packet.Properties != null)
        {
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }
        size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;

        // 订阅列表
        foreach (var sub in packet.Subscriptions)
        {
            size += MqttBinaryWriter.GetStringSize(sub.TopicFilter) + 1; // 主题 + 选项字节
        }

        return size;
    }

    public int Build(MqttSubscribePacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);
        writer.WriteUInt16(packet.PacketId);

        // 属性
        var propsSize = 0;
        if (packet.Properties?.SubscriptionIdentifier != null)
            propsSize += 1 + MqttBinaryWriter.GetVariableByteIntegerSize(packet.Properties.SubscriptionIdentifier.Value);
        if (packet.Properties != null)
        {
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }

        writer.WriteVariableByteInteger((uint)propsSize);

        if (packet.Properties?.SubscriptionIdentifier != null)
        {
            writer.WriteByte((byte)MqttPropertyId.SubscriptionIdentifier);
            writer.WriteVariableByteInteger(packet.Properties.SubscriptionIdentifier.Value);
        }
        if (packet.Properties != null)
        {
            foreach (var prop in packet.Properties.UserProperties)
            {
                writer.WriteByte((byte)MqttPropertyId.UserProperty);
                writer.WriteString(prop.Name);
                writer.WriteString(prop.Value);
            }
        }

        // 订阅列表
        foreach (var sub in packet.Subscriptions)
        {
            writer.WriteString(sub.TopicFilter);
            writer.WriteByte(sub.GetOptionsByte());
        }

        return writer.Position;
    }

    public byte GetFlags(MqttSubscribePacket packet) => 0x02;

    public void WriteTo(MqttSubscribePacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        span[0] = (byte)(((int)MqttPacketType.Subscribe << 4) | 0x02);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
