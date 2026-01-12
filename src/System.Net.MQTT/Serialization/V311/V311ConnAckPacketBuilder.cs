using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 CONNACK 报文构建器。
/// </summary>
public sealed class V311ConnAckPacketBuilder : IConnAckPacketBuilder
{
    /// <inheritdoc/>
    public MqttConnAckPacket CreateSuccess(bool sessionPresent)
    {
        return new MqttConnAckPacket
        {
            SessionPresent = sessionPresent,
            ReasonCode = 0
        };
    }

    /// <inheritdoc/>
    public MqttConnAckPacket CreateFailure(byte reasonCode, string? reasonString = null)
    {
        // MQTT 3.1.1 不支持原因字符串，忽略该参数
        return new MqttConnAckPacket
        {
            SessionPresent = false,
            ReasonCode = reasonCode
        };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalculateSize(MqttConnAckPacket packet) => 2; // 固定 2 字节

    /// <inheritdoc/>
    public int Build(MqttConnAckPacket packet, Span<byte> buffer)
    {
        buffer[0] = packet.SessionPresent ? (byte)0x01 : (byte)0x00;
        buffer[1] = packet.ReasonCode;
        return 2;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttConnAckPacket packet) => 0;

    /// <inheritdoc/>
    public void WriteTo(MqttConnAckPacket packet, IBufferWriter<byte> writer)
    {
        // CONNACK: 固定头部(2字节) + 可变头部(2字节) = 4字节
        var span = writer.GetSpan(4);
        span[0] = (byte)((int)MqttPacketType.ConnAck << 4);
        span[1] = 0x02; // 剩余长度
        span[2] = packet.SessionPresent ? (byte)0x01 : (byte)0x00;
        span[3] = packet.ReasonCode;
        writer.Advance(4);
    }
}
