using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 AUTH 报文解析器。
/// </summary>
public sealed class V500AuthPacketParser : IAuthPacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500AuthPacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttAuthPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttAuthPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var packet = new MqttAuthPacket();

        if (data.Length == 0)
        {
            packet.ReasonCode = 0;
            return packet;
        }

        var reader = new MqttBinaryReader(data);
        packet.ReasonCode = reader.ReadByte();

        if (reader.Remaining > 0)
        {
            var propertiesLength = (int)reader.ReadVariableByteInteger();
            if (propertiesLength > 0)
            {
                packet.Properties = _propertyParser.ParseAuthProperties(ref reader, propertiesLength);
            }
        }

        return packet;
    }
}
