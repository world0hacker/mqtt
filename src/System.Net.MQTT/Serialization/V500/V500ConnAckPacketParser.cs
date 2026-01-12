using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 CONNACK 报文解析器。
/// </summary>
public sealed class V500ConnAckPacketParser : IConnAckPacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500ConnAckPacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttConnAckPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttConnAckPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var reader = new MqttBinaryReader(data);
        var packet = new MqttConnAckPacket
        {
            SessionPresent = (reader.ReadByte() & 0x01) != 0,
            ReasonCode = reader.ReadByte()
        };

        if (reader.Remaining > 0)
        {
            var propertiesLength = (int)reader.ReadVariableByteInteger();
            if (propertiesLength > 0)
            {
                packet.Properties = _propertyParser.ParseConnAckProperties(ref reader, propertiesLength);
            }
        }

        return packet;
    }
}
