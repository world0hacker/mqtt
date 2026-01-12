namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 遗嘱消息属性。
/// 在 CONNECT 报文中设置，用于配置遗嘱消息的行为。
/// </summary>
public sealed class MqttWillProperties
{
    /// <summary>
    /// 遗嘱延迟间隔（秒）。
    /// 网络连接关闭后，服务器延迟发送遗嘱消息的时间。
    /// 0 表示立即发送。
    /// </summary>
    public uint? WillDelayInterval { get; set; }

    /// <summary>
    /// 载荷格式指示。
    /// 0 表示载荷是未指定格式的字节。
    /// 1 表示载荷是 UTF-8 编码的字符数据。
    /// </summary>
    public byte? PayloadFormatIndicator { get; set; }

    /// <summary>
    /// 消息过期间隔（秒）。
    /// 遗嘱消息的生存时间。
    /// </summary>
    public uint? MessageExpiryInterval { get; set; }

    /// <summary>
    /// 内容类型。
    /// 遗嘱消息载荷的 MIME 类型。
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// 响应主题。
    /// 遗嘱消息的响应主题。
    /// </summary>
    public string? ResponseTopic { get; set; }

    /// <summary>
    /// 相关数据。
    /// 遗嘱消息的相关数据。
    /// </summary>
    public ReadOnlyMemory<byte> CorrelationData { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 遗嘱消息的用户属性。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
