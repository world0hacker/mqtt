using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 SUBACK 报文解析器。
/// </summary>
public sealed class V500SubAckPacketParser : ISubAckPacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500SubAckPacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttSubAckPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttSubAckPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        if (data.Length < 3)
        {
            throw new MqttProtocolException("SUBACK 报文长度无效");
        }

        var packet = new MqttSubAckPacket();
        var reader = new MqttBinaryReader(data);

        // 报文标识符
        packet.PacketId = reader.ReadUInt16();

        // 属性
        var propertiesLength = (int)reader.ReadVariableByteInteger();
        if (propertiesLength > 0)
        {
            packet.Properties = _propertyParser.ParseSubAckProperties(ref reader, propertiesLength);
        }

        // 原因码列表
        while (reader.Remaining > 0)
        {
            packet.ReasonCodes.Add(reader.ReadByte());
        }

        return packet;
    }
}
