using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN CONNECT 报文。
/// 客户端发送以建立连接。
///
/// 格式:
/// | Length (1-3) | MsgType (1) | Flags (1) | ProtocolId (1) | Duration (2) | ClientId (n) |
/// </summary>
public sealed class MqttSnConnectPacket : IMqttSnPacket
{
    /// <summary>
    /// MQTT-SN 协议 ID。固定为 0x01。
    /// </summary>
    public const byte ProtocolIdValue = 0x01;

    /// <summary>
    /// 获取或设置标志位。
    /// </summary>
    public MqttSnFlags Flags { get; set; }

    /// <summary>
    /// 获取或设置保活时间（秒）。
    /// </summary>
    public ushort Duration { get; set; }

    /// <summary>
    /// 获取或设置客户端标识符。
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.Connect;

    /// <inheritdoc/>
    public int Length
    {
        get
        {
            var clientIdBytes = Encoding.UTF8.GetByteCount(ClientId);
            var payloadLength = 1 + 1 + 2 + clientIdBytes; // Flags + ProtocolId + Duration + ClientId
            return payloadLength <= 253 ? 2 + payloadLength : 4 + payloadLength;
        }
    }

    /// <summary>
    /// 获取是否清理会话。
    /// </summary>
    public bool CleanSession => Flags.CleanSession;

    /// <summary>
    /// 获取是否有遗嘱消息。
    /// </summary>
    public bool Will => Flags.Will;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        var clientIdBytes = Encoding.UTF8.GetBytes(ClientId);
        var payloadLength = 1 + 1 + 2 + clientIdBytes.Length;
        int offset;

        if (payloadLength <= 253)
        {
            buffer[0] = (byte)(2 + payloadLength);
            buffer[1] = (byte)MqttSnPacketType.Connect;
            offset = 2;
        }
        else
        {
            buffer[0] = 0x01;
            var totalLength = (ushort)(4 + payloadLength);
            buffer[1] = (byte)(totalLength >> 8);
            buffer[2] = (byte)totalLength;
            buffer[3] = (byte)MqttSnPacketType.Connect;
            offset = 4;
        }

        buffer[offset++] = Flags;
        buffer[offset++] = ProtocolIdValue;
        buffer[offset++] = (byte)(Duration >> 8);
        buffer[offset++] = (byte)Duration;

        clientIdBytes.CopyTo(buffer.Slice(offset));
        offset += clientIdBytes.Length;

        return offset;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="length">报文长度</param>
    /// <param name="headerLength">头部长度</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnConnectPacket Parse(ReadOnlySpan<byte> buffer, int length, int headerLength)
    {
        var dataOffset = headerLength;

        var packet = new MqttSnConnectPacket
        {
            Flags = buffer[dataOffset++]
        };

        // Skip ProtocolId (1 byte)
        dataOffset++;

        packet.Duration = (ushort)((buffer[dataOffset] << 8) | buffer[dataOffset + 1]);
        dataOffset += 2;

        var clientIdLength = length - dataOffset;
        if (clientIdLength > 0)
        {
            packet.ClientId = Encoding.UTF8.GetString(buffer.Slice(dataOffset, clientIdLength));
        }

        return packet;
    }
}
