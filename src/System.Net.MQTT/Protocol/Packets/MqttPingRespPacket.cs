namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT PINGRESP 报文。
/// 服务器对 PINGREQ 的响应。
/// </summary>
public sealed class MqttPingRespPacket : IMqttPacket
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.PingResp;

    /// <summary>
    /// 单例实例。
    /// PINGRESP 报文没有可变头部和载荷，使用单例模式节省内存。
    /// </summary>
    public static readonly MqttPingRespPacket Instance = new();

    /// <summary>
    /// 私有构造函数，使用 Instance 获取实例。
    /// </summary>
    private MqttPingRespPacket() { }
}
