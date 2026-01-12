using System.Buffers;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 CONNECT 报文构建器。
/// </summary>
public sealed class V311ConnectPacketBuilder : IConnectPacketBuilder
{
    private static readonly byte[] MqttProtocolName = Encoding.UTF8.GetBytes("MQTT");

    /// <inheritdoc/>
    public MqttConnectPacket CreateFromOptions(MqttClientOptions options)
    {
        return new MqttConnectPacket
        {
            ProtocolName = "MQTT",
            ProtocolVersion = options.ProtocolVersion == MqttProtocolVersion.V310
                ? MqttProtocolVersion.V310
                : MqttProtocolVersion.V311,
            ClientId = options.ClientId ?? $"mqtt-{Guid.NewGuid():N}",
            CleanSession = options.CleanSession,
            KeepAlive = (ushort)options.KeepAliveSeconds,
            Username = options.Username,
            Password = options.Password != null ? Encoding.UTF8.GetBytes(options.Password) : default,
            HasWill = options.WillMessage != null,
            WillTopic = options.WillMessage?.Topic,
            WillPayload = options.WillMessage?.Payload ?? default,
            WillQoS = options.WillMessage?.QualityOfService ?? MqttQualityOfService.AtMostOnce,
            WillRetain = options.WillMessage?.Retain ?? false
        };
    }

    /// <inheritdoc/>
    public int CalculateSize(MqttConnectPacket packet)
    {
        var size = 0;

        // 协议名称（2字节长度 + 4字节 "MQTT"）
        size += 2 + 4;

        // 协议版本（1字节）
        size += 1;

        // 连接标志（1字节）
        size += 1;

        // 保活时间（2字节）
        size += 2;

        // 客户端标识符
        size += MqttBinaryWriter.GetStringSize(packet.ClientId);

        // 遗嘱消息
        if (packet.HasWill)
        {
            size += MqttBinaryWriter.GetStringSize(packet.WillTopic);
            size += MqttBinaryWriter.GetBinaryDataSize(packet.WillPayload);
        }

        // 用户名
        if (!string.IsNullOrEmpty(packet.Username))
        {
            size += MqttBinaryWriter.GetStringSize(packet.Username);
        }

        // 密码
        if (!packet.Password.IsEmpty)
        {
            size += MqttBinaryWriter.GetBinaryDataSize(packet.Password);
        }

        return size;
    }

    /// <inheritdoc/>
    public int Build(MqttConnectPacket packet, Span<byte> buffer)
    {
        var writer = new MqttBinaryWriter(buffer);

        // 协议名称
        writer.WriteUInt16(4);
        writer.WriteBytes(MqttProtocolName);

        // 协议版本
        writer.WriteByte(packet.ProtocolVersion == MqttProtocolVersion.V310 ? (byte)3 : (byte)4);

        // 连接标志
        writer.WriteByte(packet.GetConnectFlags());

        // 保活时间
        writer.WriteUInt16(packet.KeepAlive);

        // 客户端标识符
        writer.WriteString(packet.ClientId);

        // 遗嘱消息
        if (packet.HasWill)
        {
            writer.WriteString(packet.WillTopic ?? string.Empty);
            writer.WriteBinaryData(packet.WillPayload.Span);
        }

        // 用户名
        if (!string.IsNullOrEmpty(packet.Username))
        {
            writer.WriteString(packet.Username);
        }

        // 密码
        if (!packet.Password.IsEmpty)
        {
            writer.WriteBinaryData(packet.Password.Span);
        }

        return writer.Position;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte GetFlags(MqttConnectPacket packet) => 0; // CONNECT 报文标志位始终为 0

    /// <inheritdoc/>
    public void WriteTo(MqttConnectPacket packet, IBufferWriter<byte> writer)
    {
        var size = CalculateSize(packet);
        var headerSize = 1 + MqttBinaryWriter.GetVariableByteIntegerSize((uint)size);
        var totalSize = headerSize + size;

        var span = writer.GetSpan(totalSize);

        // 固定头部
        span[0] = (byte)((int)MqttPacketType.Connect << 4);
        var headerWriter = new MqttBinaryWriter(span.Slice(1));
        headerWriter.WriteVariableByteInteger((uint)size);

        // 可变头部和载荷
        Build(packet, span.Slice(headerSize));

        writer.Advance(totalSize);
    }
}
