namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT PINGREQ 报文。
/// 客户端发送的心跳请求。
/// 用于保持连接活跃，检测网络状态。
/// </summary>
public sealed class MqttPingReqPacket : IMqttPacket
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.PingReq;

    /// <summary>
    /// 单例实例。
    /// PINGREQ 报文没有可变头部和载荷，使用单例模式节省内存。
    /// </summary>
    public static readonly MqttPingReqPacket Instance = new();

    /// <summary>
    /// 私有构造函数，使用 Instance 获取实例。
    /// </summary>
    private MqttPingReqPacket() { }
}
