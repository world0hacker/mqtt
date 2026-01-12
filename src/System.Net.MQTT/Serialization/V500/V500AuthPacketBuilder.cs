using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 AUTH 报文构建器。
/// </summary>
public sealed class V500AuthPacketBuilder : IAuthPacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;

    public V500AuthPacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttAuthPacket Create(byte reasonCode, string? authMethod = null, ReadOnlyMemory<byte> authData = default)
    {
        var packet = new MqttAuthPacket { ReasonCode = reasonCode };

        if (!string.IsNullOrEmpty(authMethod) || !authData.IsEmpty)
        {
            packet.Properties = new MqttAuthProperties
            {
                AuthenticationMethod = authMethod,
                AuthenticationData = authData
            };
        }

        return packet;
    }

    public int CalculateSize(MqttAuthPacket packet)
    {
        if (packet.ReasonCode == 0 && packet.Properties == null)
            return 0;

        var size = 1; // ReasonCode

        var propsSize = 0;
        if (packet.Properties != null)
        {
            if (!string.IsNullOrEmpty(packet.Properties.AuthenticationMethod))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.AuthenticationMethod);
            if (!packet.Properties.AuthenticationData.IsEmpty)
                propsSize += 1 + MqttBinaryWriter.GetBinaryDataSize(packet.Properties.AuthenticationData);
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ReasonString);
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }
        size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;

        return size;
    }

    public int Build(MqttAuthPacket packet, Span<byte> buffer)
    {
        if (packet.ReasonCode == 0 && packet.Properties == null)
            return 0;

        var writer = new MqttBinaryWriter(buffer);
        writer.WriteByte(packet.ReasonCode);

        var propsSize = 0;
        if (packet.Properties != null)
        {
            if (!string.IsNullOrEmpty(packet.Properties.AuthenticationMethod))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.AuthenticationMethod);
            if (!packet.Properties.AuthenticationData.IsEmpty)
                propsSize += 1 + MqttBinaryWriter.GetBinaryDataSize(packet.Properties.AuthenticationData);
            if (!string.IsNullOrEmpty(packet.Properties.ReasonString))
                propsSize += 1 + MqttBinaryWriter.GetStringSize(packet.Properties.ReasonString);
            foreach (var prop in packet.Properties.UserProperties)
                propsSize += 1 + MqttBinaryWriter.GetStringSize(prop.Name) + MqttBinaryWriter.GetStringSize(prop.Value);
        }

        writer.WriteVariableByteInteger((uint)propsSize);

        if (packet.Properties != null)
        {
            if (!string.IsNullOrEmpty(packet.Properties.AuthenticationMethod))
            {
                writer.WriteByte((byte)MqttPropertyId.AuthenticationMethod);
                writer.WriteString(packet.Properties.AuthenticationMethod);
            }
            if (!packet.Properties.AuthenticationData.IsEmpty)
            {
                writer.WriteByte((byte)MqttPropertyId.AuthenticationData);
                writer.WriteBinaryData(packet.Properties.AuthenticationData.Span);
            }
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

        return writer.Position;
    }

    public byte GetFlags(MqttAuthPacket packet) => 0;

    public void WriteTo(MqttAuthPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        span[0] = (byte)((int)MqttPacketType.Auth << 4);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        if (size > 0) Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
