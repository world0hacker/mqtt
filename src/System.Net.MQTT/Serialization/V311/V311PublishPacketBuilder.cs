using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 PUBLISH 报文构建器。
/// </summary>
public sealed class V311PublishPacketBuilder : IPublishPacketBuilder
{
    /// <inheritdoc/>
    public MqttPublishPacket CreateFromMessage(MqttApplicationMessage message, ushort packetId, bool duplicate = false)
    {
        return new MqttPublishPacket
        {
            Topic = message.Topic,
            Payload = message.Payload,
            QoS = message.QualityOfService,
            Retain = message.Retain,
            Duplicate = duplicate,
            PacketId = packetId
        };
    }

    /// <inheritdoc/>
    public int CalculateSize(MqttPublishPacket packet)
    {
        var size = 0;

        // 主题名称
        size += MqttBinaryWriter.GetStringSize(packet.Topic);

        // 报文标识符（仅 QoS > 0）
        if (packet.QoS != MqttQualityOfService.AtMostOnce)
        {
            size += 2;
        }

        // 载荷
        size += packet.Payload.Length;

        return size;
    }

    /// <inheritdoc/>
    public int Build(MqttPublishPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);

        // 主题名称
        writer.WriteString(packet.Topic);

        // 报文标识符（仅 QoS > 0）
        if (packet.QoS != MqttQualityOfService.AtMostOnce)
        {
            writer.WriteUInt16(packet.PacketId);
        }

        // 载荷
        writer.WriteBytes(packet.Payload.Span);

        return writer.Position;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttPublishPacket packet) => packet.GetFlags();

    /// <inheritdoc/>
    public void WriteTo(MqttPublishPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);

        // 固定头部
        span[0] = (byte)(((int)MqttPacketType.Publish << 4) | packet.GetFlags());
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);

        // 可变头部和载荷
        Build(packet, span.Slice(headerSize));

        writer.Advance(totalSize);
    }
}
