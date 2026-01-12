using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT;

/// <summary>
/// 表示 MQTT 应用消息。
/// 包含 MQTT 3.1.1 和 MQTT 5.0 的所有消息属性。
/// </summary>
public sealed class MqttApplicationMessage
{
    /// <summary>
    /// 获取或设置消息主题。
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置消息载荷。
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; set; }

    /// <summary>
    /// 获取或设置服务质量级别。
    /// </summary>
    public MqttQualityOfService QualityOfService { get; set; } = MqttQualityOfService.AtMostOnce;

    /// <summary>
    /// 获取或设置是否保留消息。
    /// </summary>
    public bool Retain { get; set; }

    #region 消息来源信息

    /// <summary>
    /// 获取或设置消息来源协议类型。
    /// 用于追踪消息是通过哪种协议发布的。
    /// </summary>
    public MqttProtocolType? SourceProtocol { get; set; }

    /// <summary>
    /// 获取或设置消息来源客户端 ID。
    /// 用于追踪消息的原始发布者。
    /// </summary>
    public string? SourceClientId { get; set; }

    /// <summary>
    /// 获取或设置消息发布时间（UTC）。
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    #endregion

    #region MQTT 5.0 属性

    /// <summary>
    /// 获取或设置载荷格式指示器。
    /// 0 = 未指定（字节数组），1 = UTF-8 编码字符串。
    /// </summary>
    public byte? PayloadFormatIndicator { get; set; }

    /// <summary>
    /// 获取或设置消息过期间隔（秒）。
    /// 消息在服务器上的存活时间。
    /// </summary>
    public uint? MessageExpiryInterval { get; set; }

    /// <summary>
    /// 获取或设置主题别名。
    /// 用于减少重复主题字符串的网络开销。
    /// </summary>
    public ushort? TopicAlias { get; set; }

    /// <summary>
    /// 获取或设置响应主题。
    /// 用于请求/响应模式。
    /// </summary>
    public string? ResponseTopic { get; set; }

    /// <summary>
    /// 获取或设置关联数据。
    /// 用于请求/响应模式，关联请求和响应。
    /// </summary>
    public ReadOnlyMemory<byte> CorrelationData { get; set; }

    /// <summary>
    /// 获取或设置内容类型。
    /// 描述载荷的 MIME 类型。
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 获取订阅标识符列表。
    /// 标识匹配的订阅。
    /// </summary>
    public IList<uint> SubscriptionIdentifiers { get; } = new List<uint>();

    /// <summary>
    /// 获取用户属性列表。
    /// 应用特定的键值对元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();

    #endregion

    /// <summary>
    /// 获取载荷的 UTF-8 字符串表示。
    /// </summary>
    public string PayloadAsString => System.Text.Encoding.UTF8.GetString(Payload.Span);

    /// <summary>
    /// 创建一个使用字符串载荷的新消息。
    /// </summary>
    public static MqttApplicationMessage Create(string topic, string payload, MqttQualityOfService qos = MqttQualityOfService.AtMostOnce, bool retain = false)
    {
        return new MqttApplicationMessage
        {
            Topic = topic,
            Payload = System.Text.Encoding.UTF8.GetBytes(payload),
            QualityOfService = qos,
            Retain = retain
        };
    }

    /// <summary>
    /// 创建一个使用字节数组载荷的新消息。
    /// </summary>
    public static MqttApplicationMessage Create(string topic, byte[] payload, MqttQualityOfService qos = MqttQualityOfService.AtMostOnce, bool retain = false)
    {
        return new MqttApplicationMessage
        {
            Topic = topic,
            Payload = payload,
            QualityOfService = qos,
            Retain = retain
        };
    }

    /// <summary>
    /// 创建一个包含 MQTT 5.0 属性的新消息。
    /// </summary>
    public static MqttApplicationMessage CreateWithProperties(
        string topic,
        ReadOnlyMemory<byte> payload,
        MqttQualityOfService qos = MqttQualityOfService.AtMostOnce,
        bool retain = false,
        string? contentType = null,
        string? responseTopic = null,
        ReadOnlyMemory<byte> correlationData = default,
        uint? messageExpiryInterval = null)
    {
        return new MqttApplicationMessage
        {
            Topic = topic,
            Payload = payload,
            QualityOfService = qos,
            Retain = retain,
            ContentType = contentType,
            ResponseTopic = responseTopic,
            CorrelationData = correlationData,
            MessageExpiryInterval = messageExpiryInterval
        };
    }
}
