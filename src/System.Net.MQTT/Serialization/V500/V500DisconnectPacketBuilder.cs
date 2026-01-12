using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 DISCONNECT 报文构建器。
/// </summary>
public sealed class V500DisconnectPacketBuilder : IDisconnectPacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;

    public V500DisconnectPacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttDisconnectPacket Create(byte reasonCode = 0, string? reasonString = null)
    {
        var packet = new MqttDisconnectPacket { ReasonCode = reasonCode };

        if (!string.IsNullOrEmpty(reasonString))
        {
            packet.Properties = new MqttDisconnectProperties { ReasonString = reasonString };
        }

        return packet;
    }

    public int CalculateSize(MqttDisconnectPacket packet)
    {
        // 优化：正常断开且无属性时剩余长度为 0
        if (packet.ReasonCode == 0 && packet.Properties == null)
            return 0;

        var size = 1; // ReasonCode

        // 属性
        var propsSize = 0;
        if (packet.Properties != null)
        {
            if (packet.Properties.SessionExpiryInterval.HasValue) propsSize += 1 + 4;
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ReasonString);
            if (!string.IsNullOrEmpty(packet.Properties.ServerReference))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ServerReference);
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }
        size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;

        return size;
    }

    public int Build(MqttDisconnectPacket packet, Span<byte> buffer)
    {
        // 优化
        if (packet.ReasonCode == 0 && packet.Properties == null)
            return 0;

        var writer = new MqttBinaryWriter(buffer);
        writer.WriteByte(packet.ReasonCode);

        // 属性
        var propsSize = 0;
        if (packet.Properties != null)
        {
            if (packet.Properties.SessionExpiryInterval.HasValue) propsSize += 1 + 4;
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ReasonString);
            if (!string.IsNullOrEmpty(packet.Properties.ServerReference))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ServerReference);
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }

        writer.WriteVariableByteInteger((uint)propsSize);

        if (packet.Properties != null)
        {
            if (packet.Properties.SessionExpiryInterval.HasValue)
            {
                writer.WriteByte((byte)MqttPropertyId.SessionExpiryInterval);
                writer.WriteUInt32(packet.Properties.SessionExpiryInterval.Value);
            }
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
            {
                writer.WriteByte((byte)MqttPropertyId.ReasonString);
                writer.WriteString(packet.Properties.ReasonString);
            }
            if (!string.IsNullOrEmpty(packet.Properties.ServerReference))
            {
                writer.WriteByte((byte)MqttPropertyId.ServerReference);
                writer.WriteString(packet.Properties.ServerReference);
            }
            foreach (var prop in packet.Properties.UserProperties)
            {
                writer.WriteByte((byte)MqttPropertyId.UserProperty);
                writer.WriteString(prop.Name);
                writer.WriteString(prop.Value);
            }
        }

        return writer.Position;
    }

    public byte GetFlags(MqttDisconnectPacket packet) => 0;

    public void WriteTo(MqttDisconnectPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        span[0] = (byte)((int)MqttPacketType.Disconnect << 4);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        if (size > 0) Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
