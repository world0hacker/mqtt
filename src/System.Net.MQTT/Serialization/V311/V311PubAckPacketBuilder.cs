using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 PUBACK/PUBREC/PUBREL/PUBCOMP 报文构建器。
/// </summary>
public sealed class V311PubAckPacketBuilder : IPubAckPacketBuilder
{
    /// <inheritdoc/>
    public MqttPubAckPacket CreatePubAck(ushort packetId, byte reasonCode = 0)
    {
        return MqttPubAckPacket.CreatePubAck(packetId, reasonCode);
    }

    /// <inheritdoc/>
    public MqttPubAckPacket CreatePubRec(ushort packetId, byte reasonCode = 0)
    {
        return MqttPubAckPacket.CreatePubRec(packetId, reasonCode);
    }

    /// <inheritdoc/>
    public MqttPubAckPacket CreatePubRel(ushort packetId, byte reasonCode = 0)
    {
        return MqttPubAckPacket.CreatePubRel(packetId, reasonCode);
    }

    /// <inheritdoc/>
    public MqttPubAckPacket CreatePubComp(ushort packetId, byte reasonCode = 0)
    {
        return MqttPubAckPacket.CreatePubComp(packetId, reasonCode);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalculateSize(MqttPubAckPacket packet) => 2; // 固定 2 字节

    /// <inheritdoc/>
    public int Build(MqttPubAckPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);
        writer.WriteUInt16(packet.PacketId);
        return 2;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttPubAckPacket packet)
    {
        // PUBREL 报文标志位为 0x02，其他为 0x00
        return packet.PacketType == MqttPacketType.PubRel ? (byte)0x02 : (byte)0x00;
    }

    /// <inheritdoc/>
    public void WriteTo(MqttPubAckPacket packet, IBufferWriter<byte> writer)
    {
        // 固定 4 字节：固定头部(2) + 报文标识符(2)
        var span = writer.GetSpan(4);
        var flags = GetFlags(packet);
        span[0] = (byte)(((int)packet.PacketType << 4) | flags);
        span[1] = 0x02; // 剩余长度
        span[2] = (byte)(packet.PacketId >> 8);
        span[3] = (byte)(packet.PacketId & 0xFF);
        writer.Advance(4);
    }
}
