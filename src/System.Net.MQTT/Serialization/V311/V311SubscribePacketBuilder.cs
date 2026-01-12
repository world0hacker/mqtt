using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 SUBSCRIBE 报文构建器。
/// </summary>
public sealed class V311SubscribePacketBuilder : ISubscribePacketBuilder
{
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public int CalculateSize(MqttSubscribePacket packet)
    {
        var size = 2; // 报文标识符

        foreach (var sub in packet.Subscriptions)
        {
            size += MqttBinaryWriter.GetStringSize(sub.TopicFilter);
            size += 1; // QoS 字节
        }

        return size;
    }

    /// <inheritdoc/>
    public int Build(MqttSubscribePacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);

        // 报文标识符
        writer.WriteUInt16(packet.PacketId);

        // 订阅列表
        foreach (var sub in packet.Subscriptions)
        {
            writer.WriteString(sub.TopicFilter);
            writer.WriteByte((byte)sub.QoS);
        }

        return writer.Position;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttSubscribePacket packet) => 0x02; // SUBSCRIBE 标志位固定为 0x02

    /// <inheritdoc/>
    public void WriteTo(MqttSubscribePacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);

        // 固定头部
        span[0] = (byte)(((int)MqttPacketType.Subscribe << 4) | 0x02);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);

        // 可变头部和载荷
        Build(packet, span.Slice(headerSize));

        writer.Advance(totalSize);
    }
}
