using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 PUBACK/PUBREC/PUBREL/PUBCOMP 报文构建器。
/// </summary>
public sealed class V500PubAckPacketBuilder : IPubAckPacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;

    public V500PubAckPacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttPubAckPacket CreatePubAck(ushort packetId, byte reasonCode = 0) =>
        MqttPubAckPacket.CreatePubAck(packetId, reasonCode);

    public MqttPubAckPacket CreatePubRec(ushort packetId, byte reasonCode = 0) =>
        MqttPubAckPacket.CreatePubRec(packetId, reasonCode);

    public MqttPubAckPacket CreatePubRel(ushort packetId, byte reasonCode = 0) =>
        MqttPubAckPacket.CreatePubRel(packetId, reasonCode);

    public MqttPubAckPacket CreatePubComp(ushort packetId, byte reasonCode = 0) =>
        MqttPubAckPacket.CreatePubComp(packetId, reasonCode);

    public int CalculateSize(MqttPubAckPacket packet)
    {
        // 优化：成功且无属性时只需 2 字节
        if (packet.ReasonCode == 0 && packet.Properties == null)
            return 2;

        var size = 3; // PacketId(2) + ReasonCode(1)

        // 属性
        if (packet.Properties != null)
        {
            // 简化：仅支持原因字符串和用户属性
            var propsSize = 0;
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ReasonString);
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);

            size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;
        }
        else
        {
            size += 1; // 属性长度 = 0
        }

        return size;
    }

    public int Build(MqttPubAckPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);
        writer.WriteUInt16(packet.PacketId);

        // 优化：成功且无属性时省略
        if (packet.ReasonCode == 0 && packet.Properties == null)
            return writer.Position;

        writer.WriteByte(packet.ReasonCode);

        if (packet.Properties != null)
        {
            var propsSize = 0;
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ReasonString);
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);

            writer.WriteVariableByteInteger((uint)propsSize);

            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
            {
                writer.WriteByte((byte)Protocol.Properties.MqttPropertyId.ReasonString);
                writer.WriteString(packet.Properties.ReasonString);
            }
            foreach (var prop in packet.Properties.UserProperties)
            {
                writer.WriteByte((byte)Protocol.Properties.MqttPropertyId.UserProperty);
                writer.WriteString(prop.Name);
                writer.WriteString(prop.Value);
            }
        }
        else
        {
            writer.WriteVariableByteInteger(0);
        }

        return writer.Position;
    }

    public byte GetFlags(MqttPubAckPacket packet) =>
        packet.PacketType == MqttPacketType.PubRel ? (byte)0x02 : (byte)0x00;

    public void WriteTo(MqttPubAckPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        var flags = GetFlags(packet);
        span[0] = (byte)(((int)packet.PacketType << 4) | flags);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
