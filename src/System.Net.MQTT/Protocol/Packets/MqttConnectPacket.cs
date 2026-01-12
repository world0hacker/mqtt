using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT CONNECT 报文。
/// 客户端连接服务器时发送的第一个报文。
/// </summary>
public sealed class MqttConnectPacket : IMqttPacket
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.Connect;

    /// <summary>
    /// 协议名称。
    /// MQTT 3.1 为 "MQIsdp"，MQTT 3.1.1 和 5.0 为 "MQTT"。
    /// </summary>
    public string ProtocolName { get; set; } = "MQTT";

    /// <summary>
    /// 协议版本。
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

    /// <summary>
    /// 客户端标识符。
    /// 唯一标识客户端的字符串。
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 清理会话标志（MQTT 3.1.1）/ 清理开始标志（MQTT 5.0）。
    /// true 表示丢弃之前的会话状态，创建新会话。
    /// </summary>
    public bool CleanSession { get; set; } = true;

    /// <summary>
    /// 保活时间（秒）。
    /// 客户端必须在此间隔内发送控制报文。
    /// 0 表示禁用保活机制。
    /// </summary>
    public ushort KeepAlive { get; set; } = 60;

    /// <summary>
    /// 用户名。
    /// 用于认证的用户名。
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码。
    /// 用于认证的密码（二进制数据）。
    /// </summary>
    public ReadOnlyMemory<byte> Password { get; set; }

    /// <summary>
    /// 是否包含遗嘱消息。
    /// </summary>
    public bool HasWill { get; set; }

    /// <summary>
    /// 遗嘱主题。
    /// 客户端异常断开时发布遗嘱消息的主题。
    /// </summary>
    public string? WillTopic { get; set; }

    /// <summary>
    /// 遗嘱消息载荷。
    /// </summary>
    public ReadOnlyMemory<byte> WillPayload { get; set; }

    /// <summary>
    /// 遗嘱消息的 QoS 级别。
    /// </summary>
    public MqttQualityOfService WillQoS { get; set; }

    /// <summary>
    /// 遗嘱消息的保留标志。
    /// </summary>
    public bool WillRetain { get; set; }

    /// <summary>
    /// MQTT 5.0 连接属性。
    /// </summary>
    public MqttConnectProperties? Properties { get; set; }

    /// <summary>
    /// MQTT 5.0 遗嘱属性。
    /// </summary>
    public MqttWillProperties? WillProperties { get; set; }

    /// <summary>
    /// 获取连接标志字节。
    /// </summary>
    /// <returns>编码后的标志字节</returns>
    public byte GetConnectFlags()
    {
        byte flags = 0;

        if (CleanSession)
            flags |= 0x02;

        if (HasWill)
        {
            flags |= 0x04;
            flags |= (byte)((int)WillQoS << 3);
            if (WillRetain)
                flags |= 0x20;
        }

        if (!string.IsNullOrEmpty(Username))
            flags |= 0x80;

        if (!Password.IsEmpty)
            flags |= 0x40;

        return flags;
    }
}
