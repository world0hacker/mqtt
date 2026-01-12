using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT SUBSCRIBE 报文。
/// 客户端订阅主题时发送。
/// </summary>
public sealed class MqttSubscribePacket : IMqttPacketWithId
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.Subscribe;

    /// <inheritdoc/>
    public ushort PacketId { get; set; }

    /// <summary>
    /// 订阅列表。
    /// 包含一个或多个主题过滤器及其订阅选项。
    /// </summary>
    public IList<MqttSubscriptionOptions> Subscriptions { get; } = new List<MqttSubscriptionOptions>();

    /// <summary>
    /// MQTT 5.0 SUBSCRIBE 属性。
    /// </summary>
    public MqttSubscribeProperties? Properties { get; set; }
}

/// <summary>
/// MQTT 5.0 订阅选项。
/// 定义单个主题订阅的参数。
/// </summary>
public sealed class MqttSubscriptionOptions
{
    /// <summary>
    /// 主题过滤器。
    /// 支持通配符（+ 和 #）。
    /// </summary>
    public string TopicFilter { get; set; } = string.Empty;

    /// <summary>
    /// 最大 QoS 级别。
    /// 服务器发送消息时使用的最高 QoS。
    /// </summary>
    public MqttQualityOfService QoS { get; set; }

    /// <summary>
    /// 不本地化标志（MQTT 5.0）。
    /// true 表示不接收自己发布的消息。
    /// </summary>
    public bool NoLocal { get; set; }

    /// <summary>
    /// 保留消息处理方式（MQTT 5.0）。
    /// </summary>
    public MqttRetainHandling RetainHandling { get; set; }

    /// <summary>
    /// 保留为已发布标志（MQTT 5.0）。
    /// true 表示服务器转发消息时保留原始的 RETAIN 标志。
    /// </summary>
    public bool RetainAsPublished { get; set; }

    /// <summary>
    /// 获取订阅选项字节（MQTT 5.0）。
    /// </summary>
    /// <returns>选项字节</returns>
    public byte GetOptionsByte()
    {
        byte options = (byte)QoS;
        if (NoLocal) options |= 0x04;
        if (RetainAsPublished) options |= 0x08;
        options |= (byte)((int)RetainHandling << 4);
        return options;
    }

    /// <summary>
    /// 从选项字节设置属性（MQTT 5.0）。
    /// </summary>
    /// <param name="options">选项字节</param>
    public void SetFromOptionsByte(byte options)
    {
        QoS = (MqttQualityOfService)(options & 0x03);
        NoLocal = (options & 0x04) != 0;
        RetainAsPublished = (options & 0x08) != 0;
        RetainHandling = (MqttRetainHandling)((options >> 4) & 0x03);
    }

    /// <summary>
    /// 创建订阅选项。
    /// </summary>
    /// <param name="topicFilter">主题过滤器</param>
    /// <param name="qos">QoS 级别</param>
    /// <returns>订阅选项</returns>
    public static MqttSubscriptionOptions Create(string topicFilter, MqttQualityOfService qos = MqttQualityOfService.AtMostOnce)
    {
        return new MqttSubscriptionOptions
        {
            TopicFilter = topicFilter,
            QoS = qos
        };
    }
}

/// <summary>
/// 保留消息处理方式（MQTT 5.0）。
/// </summary>
public enum MqttRetainHandling : byte
{
    /// <summary>
    /// 订阅时发送保留消息。
    /// </summary>
    SendAtSubscribe = 0,

    /// <summary>
    /// 仅在新订阅时发送保留消息。
    /// 如果订阅已存在，则不发送。
    /// </summary>
    SendAtSubscribeIfNew = 1,

    /// <summary>
    /// 不发送保留消息。
    /// </summary>
    DoNotSend = 2
}
