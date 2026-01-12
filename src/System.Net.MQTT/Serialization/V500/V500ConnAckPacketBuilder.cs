using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 CONNACK 报文构建器。
/// </summary>
public sealed class V500ConnAckPacketBuilder : IConnAckPacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;

    public V500ConnAckPacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttConnAckPacket CreateSuccess(bool sessionPresent)
    {
        return new MqttConnAckPacket { SessionPresent = sessionPresent, ReasonCode = 0 };
    }

    public MqttConnAckPacket CreateFailure(byte reasonCode, string? reasonString = null)
    {
        var packet = new MqttConnAckPacket { SessionPresent = false, ReasonCode = reasonCode };
        if (!string.IsNullOrEmpty(reasonString))
        {
            packet.Properties = new MqttConnAckProperties { ReasonString = reasonString };
        }
        return packet;
    }

    public int CalculateSize(MqttConnAckPacket packet)
    {
        var size = 2; // 连接确认标志 + 原因码
        var propsSize = _propertyBuilder.CalculateConnAckPropertiesSize(packet.Properties);
        size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;
        return size;
    }

    public int Build(MqttConnAckPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);
        writer.WriteByte(packet.SessionPresent ? (byte)0x01 : (byte)0x00);
        writer.WriteByte(packet.ReasonCode);
        _propertyBuilder.WriteConnAckProperties(ref writer, packet.Properties);
        return writer.Position;
    }

    public byte GetFlags(MqttConnAckPacket packet) => 0;

    public void WriteTo(MqttConnAckPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        span[0] = (byte)((int)MqttPacketType.ConnAck << 4);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
