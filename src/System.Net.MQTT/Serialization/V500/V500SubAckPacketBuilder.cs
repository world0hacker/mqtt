using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 SUBACK 报文构建器。
/// </summary>
public sealed class V500SubAckPacketBuilder : ISubAckPacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;

    public V500SubAckPacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttSubAckPacket Create(ushort packetId, IReadOnlyList<byte> reasonCodes)
    {
        var packet = new MqttSubAckPacket { PacketId = packetId };

        foreach (var code in reasonCodes)
        {
            packet.ReasonCodes.Add(code);
        }

        return packet;
    }

    public int CalculateSize(MqttSubAckPacket packet)
    {
        var size = 2; // PacketId

        // 属性
        var propsSize = 0;
        if (packet.Properties != null)
        {
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ReasonString);
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }
        size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;

        // 原因码列表
        size += packet.ReasonCodes.Count;

        return size;
    }

    public int Build(MqttSubAckPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);
        writer.WriteUInt16(packet.PacketId);

        // 属性
        var propsSize = 0;
        if (packet.Properties != null)
        {
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ReasonString);
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }

        writer.WriteVariableByteInteger((uint)propsSize);

        if (packet.Properties != null)
        {
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
            {
                writer.WriteByte((byte)MqttPropertyId.ReasonString);
                writer.WriteString(packet.Properties.ReasonString);
            }
            foreach (var prop in packet.Properties.UserProperties)
            {
                writer.WriteByte((byte)MqttPropertyId.UserProperty);
                writer.WriteString(prop.Name);
                writer.WriteString(prop.Value);
            }
        }

        // 原因码列表
        foreach (var code in packet.ReasonCodes)
        {
            writer.WriteByte(code);
        }

        return writer.Position;
    }

    public byte GetFlags(MqttSubAckPacket packet) => 0;

    public void WriteTo(MqttSubAckPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        span[0] = (byte)((int)MqttPacketType.SubAck << 4);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
