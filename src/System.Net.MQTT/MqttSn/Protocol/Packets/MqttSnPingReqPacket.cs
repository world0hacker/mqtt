using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN PINGREQ 报文。
/// 心跳请求或睡眠客户端唤醒请求。
///
/// 格式:
/// | Length (1-3) | MsgType (1) | ClientId (0 or n) |
///
/// 当包含 ClientId 时，表示睡眠客户端请求接收缓存的消息。
/// </summary>
public sealed class MqttSnPingReqPacket : IMqttSnPacket
{
    /// <summary>
    /// 获取或设置客户端标识符（可选）。
    /// 仅当睡眠客户端唤醒时使用。
    /// </summary>
    public string? ClientId { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.PingReq;

    /// <inheritdoc/>
    public int Length
    {
        get
        {
            if (string.IsNullOrEmpty(ClientId))
            {
                return 2; // Length + MsgType
            }

            var clientIdBytes = Encoding.UTF8.GetByteCount(ClientId);
            return clientIdBytes <= 253 ? 2 + clientIdBytes : 4 + clientIdBytes;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        if (string.IsNullOrEmpty(ClientId))
        {
            buffer[0] = 2;
            buffer[1] = (byte)MqttSnPacketType.PingReq;
            return 2;
        }

        var clientIdBytes = Encoding.UTF8.GetBytes(ClientId);
        int offset;

        if (clientIdBytes.Length <= 253)
        {
            buffer[0] = (byte)(2 + clientIdBytes.Length);
            buffer[1] = (byte)MqttSnPacketType.PingReq;
            offset = 2;
        }
        else
        {
            buffer[0] = 0x01;
            var totalLength = (ushort)(4 + clientIdBytes.Length);
            buffer[1] = (byte)(totalLength >> 8);
            buffer[2] = (byte)totalLength;
            buffer[3] = (byte)MqttSnPacketType.PingReq;
            offset = 4;
        }

        clientIdBytes.CopyTo(buffer.Slice(offset));
        return offset + clientIdBytes.Length;
    }

    /// <summary>
    /// 从缓冲区解析报文。
    /// </summary>
    /// <param name="buffer">数据缓冲区</param>
    /// <param name="length">报文长度</param>
    /// <param name="headerLength">头部长度</param>
    /// <returns>解析的报文</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnPingReqPacket Parse(ReadOnlySpan<byte> buffer, int length, int headerLength)
    {
        var packet = new MqttSnPingReqPacket();
        var clientIdLength = length - headerLength;

        if (clientIdLength > 0)
        {
            packet.ClientId = Encoding.UTF8.GetString(buffer.Slice(headerLength, clientIdLength));
        }

        return packet;
    }
}
