using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 PUBACK/PUBREC/PUBREL/PUBCOMP 报文解析器。
/// </summary>
public sealed class V311PubAckPacketParser : IPubAckPacketParser
{
    /// <inheritdoc/>
    public MqttPubAckPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment)
        {
            return Parse(data.FirstSpan, flags);
        }

        var array = data.ToArray();
        return Parse(array.AsSpan(), flags);
    }

    /// <inheritdoc/>
    public MqttPubAckPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        if (data.Length < 2)
        {
            throw new MqttProtocolException("PUBACK/PUBREC/PUBREL/PUBCOMP 报文长度无效");
        }

        var reader = new MqttBinaryReader(data);
        return new MqttPubAckPacket
        {
            PacketId = reader.ReadUInt16(),
            ReasonCode = 0 // MQTT 3.1.1 没有原因码
        };
    }
}
