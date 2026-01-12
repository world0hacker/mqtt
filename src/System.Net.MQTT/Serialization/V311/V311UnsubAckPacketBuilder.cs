using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 UNSUBACK 报文构建器。
/// </summary>
public sealed class V311UnsubAckPacketBuilder : IUnsubAckPacketBuilder
{
    /// <inheritdoc/>
    public MqttUnsubAckPacket Create(ushort packetId, IReadOnlyList<byte>? reasonCodes = null)
    {
        // MQTT 3.1.1 不支持原因码，忽略该参数
        return new MqttUnsubAckPacket { PacketId = packetId };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalculateSize(MqttUnsubAckPacket packet) => 2; // 固定 2 字节

    /// <inheritdoc/>
    public int Build(MqttUnsubAckPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);
        writer.WriteUInt16(packet.PacketId);
        return 2;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttUnsubAckPacket packet) => 0;

    /// <inheritdoc/>
    public void WriteTo(MqttUnsubAckPacket packet, IBufferWriter<byte> writer)
    {
        // UNSUBACK: 固定头部(2) + 报文标识符(2) = 4字节
        var span = writer.GetSpan(4);
        span[0] = (byte)((int)MqttPacketType.UnsubAck << 4);
        span[1] = 0x02; // 剩余长度
        span[2] = (byte)(packet.PacketId >> 8);
        span[3] = (byte)(packet.PacketId & 0xFF);
        writer.Advance(4);
    }
}
