using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 CONNECT 报文解析器。
/// </summary>
public sealed class V500ConnectPacketParser : IConnectPacketParser
{
    private readonly V500PropertyParser _propertyParser;

    public V500ConnectPacketParser(V500PropertyParser propertyParser)
    {
        _propertyParser = propertyParser;
    }

    public MqttConnectPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment) return Parse(data.FirstSpan, flags);
        return Parse(data.ToArray().AsSpan(), flags);
    }

    public MqttConnectPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var reader = new MqttBinaryReader(data);
        var packet = new MqttConnectPacket();

        packet.ProtocolName = reader.ReadString();
        var protocolLevel = reader.ReadByte();
        packet.ProtocolVersion = protocolLevel == 5 ? MqttProtocolVersion.V500 : throw new MqttProtocolException($"期望 MQTT 5.0，但收到版本 {protocolLevel}");

        var connectFlags = reader.ReadByte();
        packet.CleanSession = (connectFlags & 0x02) != 0;
        packet.HasWill = (connectFlags & 0x04) != 0;
        packet.WillQoS = (MqttQualityOfService)((connectFlags >> 3) & 0x03);
        packet.WillRetain = (connectFlags & 0x20) != 0;
        var hasPassword = (connectFlags & 0x40) != 0;
        var hasUsername = (connectFlags & 0x80) != 0;

        packet.KeepAlive = reader.ReadUInt16();

        // MQTT 5.0 属性
        var propertiesLength = (int)reader.ReadVariableByteInteger();
        if (propertiesLength > 0)
        {
            packet.Properties = _propertyParser.ParseConnectProperties(ref reader, propertiesLength);
        }

        packet.ClientId = reader.ReadString();

        if (packet.HasWill)
        {
            // 遗嘱属性
            var willPropertiesLength = (int)reader.ReadVariableByteInteger();
            if (willPropertiesLength > 0)
            {
                // 简化处理，跳过遗嘱属性
                reader.Skip(willPropertiesLength);
            }
            packet.WillTopic = reader.ReadString();
            packet.WillPayload = reader.ReadBinaryData();
        }

        if (hasUsername) packet.Username = reader.ReadString();
        if (hasPassword) packet.Password = reader.ReadBinaryData();

        return packet;
    }
}
