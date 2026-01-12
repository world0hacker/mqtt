using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 DISCONNECT 报文解析器。
/// </summary>
public sealed class V500DisconnectPacketParser : IDisconnectPacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500DisconnectPacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttDisconnectPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttDisconnectPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var packet = new MqttDisconnectPacket();

        // 剩余长度为 0 时，原因码默认为 0（正常断开）
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
                packet.Properties = _propertyParser.ParseDisconnectProperties(ref reader, propertiesLength);
            }
        }

        return packet;
    }
}
