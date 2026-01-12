using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.MqttSn.Protocol.Packets;

/// <summary>
/// MQTT-SN GWINFO 报文。
/// 响应 SEARCHGW 请求，提供网关信息。
///
/// 格式:
/// | Length (1-3) | MsgType (1) | GwId (1) | GwAdd (0-n) |
/// </summary>
public sealed class MqttSnGwInfoPacket : IMqttSnPacket
{
    /// <summary>
    /// 获取或设置网关 ID。
    /// </summary>
    public byte GatewayId { get; set; }

    /// <summary>
    /// 获取或设置网关地址（可选）。
    /// 当由网关发送时通常为空，当由客户端转发时包含网关地址。
    /// </summary>
    public byte[]? GatewayAddress { get; set; }

    /// <inheritdoc/>
    public MqttSnPacketType PacketType => MqttSnPacketType.GwInfo;

    /// <inheritdoc/>
    public int Length
    {
        get
        {
            var payloadLength = 1 + (GatewayAddress?.Length ?? 0); // GwId + GwAdd
            return payloadLength <= 253 ? 2 + payloadLength : 4 + payloadLength;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WriteTo(Span<byte> buffer)
    {
        var payloadLength = 1 + (GatewayAddress?.Length ?? 0);
        int offset;

        if (payloadLength <= 253)
        {
            buffer[0] = (byte)(2 + payloadLength);
            buffer[1] = (byte)MqttSnPacketType.GwInfo;
            offset = 2;
        }
        else
        {
            buffer[0] = 0x01;
            var totalLength = (ushort)(4 + payloadLength);
            buffer[1] = (byte)(totalLength >> 8);
            buffer[2] = (byte)totalLength;
            buffer[3] = (byte)MqttSnPacketType.GwInfo;
            offset = 4;
        }

        buffer[offset++] = GatewayId;

        if (GatewayAddress != null && GatewayAddress.Length > 0)
        {
            GatewayAddress.CopyTo(buffer.Slice(offset));
            offset += GatewayAddress.Length;
        }

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
    public static MqttSnGwInfoPacket Parse(ReadOnlySpan<byte> buffer, int length, int headerLength)
    {
        var dataOffset = headerLength;
        var packet = new MqttSnGwInfoPacket
        {
            GatewayId = buffer[dataOffset++]
        };

        var addressLength = length - headerLength - 1;
        if (addressLength > 0)
        {
            packet.GatewayAddress = buffer.Slice(dataOffset, addressLength).ToArray();
        }

        return packet;
    }
}
