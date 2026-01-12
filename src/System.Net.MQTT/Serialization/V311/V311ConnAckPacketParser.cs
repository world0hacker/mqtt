using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 CONNACK 报文解析器。
/// </summary>
public sealed class V311ConnAckPacketParser : IConnAckPacketParser
{
    /// <inheritdoc/>
    public MqttConnAckPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment)
        {
            return Parse(data.FirstSpan, flags);
        }

        var array = data.ToArray();
        return Parse(array.AsSpan(), flags);
    }

    /// <inheritdoc/>
    public MqttConnAckPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        if (data.Length < 2)
        {
            throw new MqttProtocolException("CONNACK 报文长度无效");
        }

        return new MqttConnAckPacket
        {
            SessionPresent = (data[0] & 0x01) != 0,
            ReasonCode = data[1]
        };
    }
}
