using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 SUBACK 报文构建器。
/// </summary>
public sealed class V311SubAckPacketBuilder : ISubAckPacketBuilder
{
    /// <inheritdoc/>
    public MqttSubAckPacket Create(ushort packetId, IReadOnlyList<byte> reasonCodes)
    {
        var packet = new MqttSubAckPacket { PacketId = packetId };

        foreach (var code in reasonCodes)
        {
            packet.ReasonCodes.Add(code);
        }

        return packet;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalculateSize(MqttSubAckPacket packet) => 2 + packet.ReasonCodes.Count;

    /// <inheritdoc/>
    public int Build(MqttSubAckPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);

        // 报文标识符
        writer.WriteUInt16(packet.PacketId);

        // 返回码列表
        foreach (var code in packet.ReasonCodes)
        {
            writer.WriteByte(code);
        }

        return writer.Position;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttSubAckPacket packet) => 0;

    /// <inheritdoc/>
    public void WriteTo(MqttSubAckPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);

        // 固定头部
        span[0] = (byte)((int)MqttPacketType.SubAck << 4);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);

        // 可变头部和载荷
        Build(packet, span.Slice(headerSize));

        writer.Advance(totalSize);
    }
}
