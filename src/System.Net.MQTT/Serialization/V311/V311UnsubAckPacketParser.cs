using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 UNSUBACK 报文解析器。
/// </summary>
public sealed class V311UnsubAckPacketParser : IUnsubAckPacketParser
{
    /// <inheritdoc/>
    public MqttUnsubAckPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment)
        {
            return Parse(data.FirstSpan, flags);
        }

        var array = data.ToArray();
        return Parse(array.AsSpan(), flags);
    }

    /// <inheritdoc/>
    public MqttUnsubAckPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        if (data.Length < 2)
        {
            throw new MqttProtocolException("UNSUBACK 报文长度无效");
        }

        var reader = new MqttBinaryReader(data);

        // MQTT 3.1.1 UNSUBACK 只有报文标识符，没有原因码
        return new MqttUnsubAckPacket
        {
            PacketId = reader.ReadUInt16()
        };
    }
}
