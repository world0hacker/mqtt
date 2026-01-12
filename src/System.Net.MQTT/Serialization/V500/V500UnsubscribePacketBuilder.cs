using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 UNSUBSCRIBE 报文构建器。
/// </summary>
public sealed class V500UnsubscribePacketBuilder : IUnsubscribePacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;

    public V500UnsubscribePacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttUnsubscribePacket Create(ushort packetId, IReadOnlyList<string> topicFilters)
    {
        var packet = new MqttUnsubscribePacket { PacketId = packetId };

        foreach (var topic in topicFilters)
        {
            packet.TopicFilters.Add(topic);
        }

        return packet;
    }

    public int CalculateSize(MqttUnsubscribePacket packet)
    {
        var size = 2; // PacketId

        // 属性
        var propsSize = 0;
        if (packet.Properties != null)
        {
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }
        size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;

        // 主题过滤器列表
        foreach (var topic in packet.TopicFilters)
        {
            size += MqttBinaryWriter.GetStringSize(topic);
        }

        return size;
    }

    public int Build(MqttUnsubscribePacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);
        writer.WriteUInt16(packet.PacketId);

        // 属性
        var propsSize = 0;
        if (packet.Properties != null)
        {
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }

        writer.WriteVariableByteInteger((uint)propsSize);

        if (packet.Properties != null)
        {
            foreach (var prop in packet.Properties.UserProperties)
            {
                writer.WriteByte((byte)MqttPropertyId.UserProperty);
                writer.WriteString(prop.Name);
                writer.WriteString(prop.Value);
            }
        }

        // 主题过滤器列表
        foreach (var topic in packet.TopicFilters)
        {
            writer.WriteString(topic);
        }

        return writer.Position;
    }

    public byte GetFlags(MqttUnsubscribePacket packet) => 0x02;

    public void WriteTo(MqttUnsubscribePacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        span[0] = (byte)(((int)MqttPacketType.Unsubscribe << 4) | 0x02);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
