using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 CONNECT 报文解析器。
/// </summary>
public sealed class V311ConnectPacketParser : IConnectPacketParser
{
    /// <inheritdoc/>
    public MqttConnectPacket Parse(ReadOnlySequence<byte> data, byte flags)
    {
        // 对于连续内存，使用 Span 版本
        if (data.IsSingleSegment)
        {
            return Parse(data.FirstSpan, flags);
        }

        // 非连续内存，复制到临时数组
        var array = data.ToArray();
        return Parse(array.AsSpan(), flags);
    }

    /// <inheritdoc/>
    public MqttConnectPacket Parse(ReadOnlySpan<byte> data, byte flags)
    {
        var reader = new MqttBinaryReader(data);
        var packet = new MqttConnectPacket();

        // 协议名称
        packet.ProtocolName = reader.ReadString();

        // 协议版本
        var protocolLevel = reader.ReadByte();
        packet.ProtocolVersion = protocolLevel switch
        {
            3 => MqttProtocolVersion.V310,
            4 => MqttProtocolVersion.V311,
            _ => throw new MqttProtocolException($"不支持的协议版本: {protocolLevel}")
        };

        // 连接标志
        var connectFlags = reader.ReadByte();
        packet.CleanSession = (connectFlags & 0x02) != 0;
        packet.HasWill = (connectFlags & 0x04) != 0;
        packet.WillQoS = (MqttQualityOfService)((connectFlags >> 3) & 0x03);
        packet.WillRetain = (connectFlags & 0x20) != 0;
        var hasPassword = (connectFlags & 0x40) != 0;
        var hasUsername = (connectFlags & 0x80) != 0;

        // 保活时间
        packet.KeepAlive = reader.ReadUInt16();

        // 客户端标识符
        packet.ClientId = reader.ReadString();

        // 遗嘱消息
        if (packet.HasWill)
        {
            packet.WillTopic = reader.ReadString();
            packet.WillPayload = reader.ReadBinaryData();
        }

        // 用户名
        if (hasUsername)
        {
            packet.Username = reader.ReadString();
        }

        // 密码
        if (hasPassword)
        {
            packet.Password = reader.ReadBinaryData();
        }

        return packet;
    }
}
