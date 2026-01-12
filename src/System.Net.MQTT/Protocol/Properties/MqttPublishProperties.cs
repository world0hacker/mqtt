namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 PUBLISH 报文属性。
/// 包含消息的元数据和应用层属性。
/// </summary>
public sealed class MqttPublishProperties
{
    /// <summary>
    /// 载荷格式指示。
    /// 0 表示载荷是未指定格式的字节。
    /// 1 表示载荷是 UTF-8 编码的字符数据。
    /// </summary>
    public byte? PayloadFormatIndicator { get; set; }

    /// <summary>
    /// 消息过期间隔（秒）。
    /// 消息的生存时间。
    /// 如果消息在服务器上存储的时间超过此值，服务器应删除该消息。
    /// </summary>
    public uint? MessageExpiryInterval { get; set; }

    /// <summary>
    /// 主题别名。
    /// 用于减少主题名称传输开销的别名值。
    /// 范围：1-65535。
    /// </summary>
    public ushort? TopicAlias { get; set; }

    /// <summary>
    /// 响应主题。
    /// 请求/响应模式中，接收方应该向此主题发送响应。
    /// </summary>
    public string? ResponseTopic { get; set; }

    /// <summary>
    /// 相关数据。
    /// 请求/响应模式中，用于关联请求和响应的二进制数据。
    /// </summary>
    public ReadOnlyMemory<byte> CorrelationData { get; set; }

    /// <summary>
    /// 订阅标识符列表。
    /// 消息匹配的订阅标识符。
    /// 服务器在转发消息时添加此属性。
    /// </summary>
    public IList<uint> SubscriptionIdentifiers { get; } = new List<uint>();

    /// <summary>
    /// 内容类型。
    /// 载荷的 MIME 类型，如 "application/json"、"text/plain" 等。
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 应用特定的键值对元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
