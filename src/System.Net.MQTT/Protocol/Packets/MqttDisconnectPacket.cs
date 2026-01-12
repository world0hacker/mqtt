using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT DISCONNECT 报文。
/// 用于通知对端连接即将关闭。
/// MQTT 3.1.1 中只有客户端可以发送。
/// MQTT 5.0 中客户端和服务器都可以发送。
/// </summary>
public sealed class MqttDisconnectPacket : IMqttPacket
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.Disconnect;

    /// <summary>
    /// 原因码（MQTT 5.0）。
    /// 0x00 表示正常断开。
    /// </summary>
    public byte ReasonCode { get; set; }

    /// <summary>
    /// MQTT 5.0 DISCONNECT 属性。
    /// </summary>
    public MqttDisconnectProperties? Properties { get; set; }

    /// <summary>
    /// 判断是否为正常断开。
    /// </summary>
    public bool IsNormalDisconnect => ReasonCode == 0;
}
