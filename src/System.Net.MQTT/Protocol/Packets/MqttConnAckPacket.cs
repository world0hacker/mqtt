using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT CONNACK 报文。
/// 服务器对客户端 CONNECT 请求的响应。
/// </summary>
public sealed class MqttConnAckPacket : IMqttPacket
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.ConnAck;

    /// <summary>
    /// 会话存在标志。
    /// true 表示服务器已有该客户端的会话状态。
    /// 仅在 CleanSession=0 时有意义。
    /// </summary>
    public bool SessionPresent { get; set; }

    /// <summary>
    /// 返回码（MQTT 3.1.1）或原因码（MQTT 5.0）。
    /// 0 表示连接成功。
    /// </summary>
    public byte ReasonCode { get; set; }

    /// <summary>
    /// MQTT 5.0 CONNACK 属性。
    /// </summary>
    public MqttConnAckProperties? Properties { get; set; }

    /// <summary>
    /// 判断连接是否成功。
    /// </summary>
    public bool IsSuccess => ReasonCode == 0;
}
