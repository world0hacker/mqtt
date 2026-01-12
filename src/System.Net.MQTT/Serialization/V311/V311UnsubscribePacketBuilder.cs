using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 UNSUBSCRIBE 报文构建器。
/// </summary>
public sealed class V311UnsubscribePacketBuilder : IUnsubscribePacketBuilder
{
    /// <inheritdoc/>
    public MqttUnsubscribePacket Create(ushort packetId, IReadOnlyList<string> topicFilters)
    {
        var packet = new MqttUnsubscribePacket { PacketId = packetId };

        foreach (var topic in topicFilters)
        {
            packet.TopicFilters.Add(topic);
        }

        return packet;
    }

    /// <inheritdoc/>
    public int CalculateSize(MqttUnsubscribePacket packet)
    {
        var size = 2; // 报文标识符

        foreach (var topic in packet.TopicFilters)
        {
            size += MqttBinaryWriter.GetStringSize(topic);
        }

        return size;
    }

    /// <inheritdoc/>
    public int Build(MqttUnsubscribePacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);

        // 报文标识符
        writer.WriteUInt16(packet.PacketId);

        // 主题过滤器列表
        foreach (var topic in packet.TopicFilters)
        {
            writer.WriteString(topic);
        }

        return writer.Position;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttUnsubscribePacket packet) => 0x02; // UNSUBSCRIBE 标志位固定为 0x02

    /// <inheritdoc/>
    public void WriteTo(MqttUnsubscribePacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);

        // 固定头部
        span[0] = (byte)(((int)MqttPacketType.Unsubscribe << 4) | 0x02);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);

        // 可变头部和载荷
        Build(packet, span.Slice(headerSize));

        writer.Advance(totalSize);
    }
}
