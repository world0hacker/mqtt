using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 DISCONNECT 报文解析器。
/// </summary>
public sealed class V311DisconnectPacketParser : IDisconnectPacketParser
{
    /// <inheritdoc/>
    public MqttDisconnectPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        // MQTT 3.1.1 DISCONNECT 没有可变头部和载荷
        return new MqttDisconnectPacket { ReasonCode = 0 };
    }

    /// <inheritdoc/>
    public MqttDisconnectPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        // MQTT 3.1.1 DISCONNECT 没有可变头部和载荷
        return new MqttDisconnectPacket { ReasonCode = 0 };
    }
}
