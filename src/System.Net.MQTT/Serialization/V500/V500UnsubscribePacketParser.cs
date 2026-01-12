using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 UNSUBSCRIBE 报文解析器。
/// </summary>
public sealed class V500UnsubscribePacketParser : IUnsubscribePacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500UnsubscribePacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttUnsubscribePacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttUnsubscribePacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var packet = new MqttUnsubscribePacket();
        var reader = new MqttBinaryReader(data);

        // 报文标识符
        packet.PacketId = reader.ReadUInt16();

        // 属性
        var propertiesLength = (int)reader.ReadVariableByteInteger();
        if (propertiesLength > 0)
        {
            packet.Properties = _propertyParser.ParseUnsubscribeProperties(ref reader, propertiesLength);
        }

        // 主题过滤器列表
        while (reader.Remaining > 0)
        {
            packet.TopicFilters.Add(reader.ReadString());
        }

        if (packet.TopicFilters.Count == 0)
        {
            throw new MqttProtocolException("UNSUBSCRIBE 报文必须包含至少一个主题过滤器");
        }

        return packet;
    }
}
