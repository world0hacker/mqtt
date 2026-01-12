using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 SUBACK 报文解析器。
/// </summary>
public sealed class V311SubAckPacketParser : ISubAckPacketParser
{
    /// <inheritdoc/>
    public MqttSubAckPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        if (data.IsSingleSegment)
        {
            return Parse(data.FirstSpan, flags);
        }

        var array = data.ToArray();
        return Parse(array.AsSpan(), flags);
    }

    /// <inheritdoc/>
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

        // 返回码列表
        while (reader.Remaining > 0)
        {
            packet.ReasonCodes.Add(reader.ReadByte());
        }

        return packet;
    }
}
