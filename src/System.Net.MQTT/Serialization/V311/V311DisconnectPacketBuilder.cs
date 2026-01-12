using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 DISCONNECT 报文构建器。
/// </summary>
public sealed class V311DisconnectPacketBuilder : IDisconnectPacketBuilder
{
    // 预分配的 DISCONNECT 报文字节
    private static readonly byte[] DisconnectBytes = { 0xE0, 0x00 };

    /// <inheritdoc/>
    public MqttDisconnectPacket Create(byte reasonCode = 0, string? reasonString = null)
    {
        // MQTT 3.1.1 不支持原因码和原因字符串，忽略这些参数
        return new MqttDisconnectPacket { ReasonCode = 0 };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CalculateSize(MqttDisconnectPacket packet) => 0; // 没有可变头部和载荷

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Build(MqttDisconnectPacket packet, Span<byte> buffer) => 0;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttDisconnectPacket packet) => 0;

    /// <inheritdoc/>
    public void WriteTo(MqttDisconnectPacket packet, IBufferWriter<byte> writer)
    {
        // DISCONNECT: 固定 2 字节
        var span = writer.GetSpan(2);
        span[0] = 0xE0; // DISCONNECT 类型 = 14
        span[1] = 0x00; // 剩余长度 = 0
        writer.Advance(2);
    }
}
