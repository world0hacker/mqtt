using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 PUBACK/PUBREC/PUBREL/PUBCOMP 报文解析器。
/// </summary>
public sealed class V500PubAckPacketParser : IPubAckPacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500PubAckPacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttPubAckPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttPubAckPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var reader = new MqttBinaryReader(data);
        var packet = new MqttPubAckPacket
        {
            PacketId = reader.ReadUInt16()
        };

        // MQTT 5.0: 如果剩余长度为 2，则没有原因码和属性
        if (reader.Remaining > 0)
        {
            packet.ReasonCode = reader.ReadByte();

            if (reader.Remaining > 0)
            {
                var propertiesLength = (int)reader.ReadVariableByteInteger();
                if (propertiesLength > 0)
                {
                    packet.Properties = _propertyParser.ParsePubAckProperties(ref reader, propertiesLength);
                }
            }
        }

        return packet;
    }
}
