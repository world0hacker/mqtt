using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 PUBLISH 报文解析器。
/// </summary>
public sealed class V311PublishPacketParser : IPublishPacketParser
{
    /// <inheritdoc/>
    public MqttPublishPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment)
        {
            return Parse(data.FirstSpan, flags);
        }

        var array = data.ToArray();
        return Parse(array.AsSpan(), flags);
    }

    /// <inheritdoc/>
    public MqttPublishPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var packet = new MqttPublishPacket();
        packet.SetFromFlags(flags);

        var reader = new MqttBinaryReader(data);

        // 主题名称
        packet.Topic = reader.ReadString();

        // 报文标识符（仅 QoS > 0）
        if (packet.QoS != MqttQualityOfService.AtMostOnce)
        {
            packet.PacketId = reader.ReadUInt16();
        }

        // 载荷（剩余字节）
        packet.Payload = reader.ReadRemainingBytesAsMemory();

        return packet;
    }

    /// <inheritdoc/>
    public MqttApplicationMessage ToApplicationMessage(MqttPublishPacket packet)
    {
        return new MqttApplicationMessage
        {
            Topic = packet.Topic,
            Payload = packet.Payload,
            QualityOfService = packet.QoS,
            Retain = packet.Retain
        };
    }
}
