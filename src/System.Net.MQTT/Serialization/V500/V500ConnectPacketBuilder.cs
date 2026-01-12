using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Text;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 CONNECT 报文构建器。
/// </summary>
public sealed class V500ConnectPacketBuilder : IConnectPacketBuilder
{
    private readonly V500PropertyBuilder _propertyBuilder;
    private static readonly byte[] MqttProtocolName = Encoding.UTF8.GetBytes("MQTT");

    public V500ConnectPacketBuilder(V500PropertyBuilder propertyBuilder)
    {
        _propertyBuilder = propertyBuilder;
    }

    public MqttConnectPacket CreateFromOptions(MqttClientOptions options)
    {
        return new MqttConnectPacket
        {
            ProtocolName = "MQTT",
            ProtocolVersion = MqttProtocolVersion.V500,
            ClientId = options.ClientId ?? $"mqtt-{Guid.NewGuid():N}",
            CleanSession = options.CleanSession,
            KeepAlive = (ushort)options.KeepAliveSeconds,
            Username = options.Username,
            Password = options.Password != null ? Encoding.UTF8.GetBytes(options.Password) : default,
            HasWill = options.WillMessage != null,
            WillTopic = options.WillMessage?.Topic,
            WillPayload = options.WillMessage?.Payload ?? default,
            WillQoS = options.WillMessage?.QualityOfService ?? MqttQualityOfService.AtMostOnce,
            WillRetain = options.WillMessage?.Retain ?? false
        };
    }

    public int CalculateSize(MqttConnectPacket packet)
    {
        var size = 2 + 4 + 1 + 1 + 2; // 协议名称 + 版本 + 标志 + 保活

        // 属性长度
        var propsSize = _propertyBuilder.CalculateConnectPropertiesSize(packet.Properties);
        size += MqttBinaryWriter.GetVariableByteIntegerSize((uint)propsSize) + propsSize;

        size += MqttBinaryWriter.GetStringSize(packet.ClientId);

        if (packet.HasWill)
        {
            size += 1; // 遗嘱属性长度（简化为 0）
            size += MqttBinaryWriter.GetStringSize(packet.WillTopic);
            size += MqttBinaryWriter.GetBinaryDataSize(packet.WillPayload);
        }

        if (!string.IsNullOrEmpty(packet.Username))
            size += MqttBinaryWriter.GetStringSize(packet.Username);
        if (!packet.Password.IsEmpty)
            size += MqttBinaryWriter.GetBinaryDataSize(packet.Password);

        return size;
    }

    public int Build(MqttConnectPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);

        writer.WriteUInt16(4);
        writer.WriteBytes(MqttProtocolName);
        writer.WriteByte(5); // MQTT 5.0
        writer.WriteByte(packet.GetConnectFlags());
        writer.WriteUInt16(packet.KeepAlive);

        _propertyBuilder.WriteConnectProperties(ref writer, packet.Properties);

        writer.WriteString(packet.ClientId);

        if (packet.HasWill)
        {
            writer.WriteVariableByteInteger(0); // 遗嘱属性长度
            writer.WriteString(packet.WillTopic ?? string.Empty);
            writer.WriteBinaryData(packet.WillPayload.Span);
        }

        if (!string.IsNullOrEmpty(packet.Username))
            writer.WriteString(packet.Username);
        if (!packet.Password.IsEmpty)
            writer.WriteBinaryData(packet.Password.Span);

        return writer.Position;
    }

    public byte GetFlags(MqttConnectPacket packet) => 0;

    public void WriteTo(MqttConnectPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);
        span[0] = (byte)((int)MqttPacketType.Connect << 4);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);
        Build(packet, span.Slice(headerSize));
        writer.Advance(totalSize);
    }
}
