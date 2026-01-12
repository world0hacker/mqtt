namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 CONNACK 报文属性。
/// 服务器在确认连接时返回给客户端的属性集合。
/// </summary>
public sealed class MqttConnAckProperties
{
    /// <summary>
    /// 会话过期间隔（秒）。
    /// 服务器允许的会话过期间隔。
    /// 如果未设置，使用客户端请求的值。
    /// </summary>
    public uint? SessionExpiryInterval { get; set; }

    /// <summary>
    /// 接收最大值。
    /// 服务器愿意同时处理的未确认 QoS 1 和 QoS 2 PUBLISH 报文数量。
    /// 范围：1-65535。
    /// </summary>
    public ushort? ReceiveMaximum { get; set; }

    /// <summary>
    /// 最大 QoS。
    /// 服务器支持的最高 QoS 级别（0、1 或 2）。
    /// 客户端不能发送超过此级别的 PUBLISH 报文。
    /// </summary>
    public MqttQualityOfService? MaximumQoS { get; set; }

    /// <summary>
    /// 保留消息可用。
    /// true 表示服务器支持保留消息。
    /// false 表示服务器不支持保留消息，客户端不能发送 RETAIN=1 的消息。
    /// </summary>
    public bool? RetainAvailable { get; set; }

    /// <summary>
    /// 最大报文大小（字节）。
    /// 服务器愿意接收的最大报文大小。
    /// 客户端不能发送超过此大小的报文。
    /// </summary>
    public uint? MaximumPacketSize { get; set; }

    /// <summary>
    /// 分配的客户端标识符。
    /// 当客户端使用空客户端 ID 连接时，服务器分配的客户端 ID。
    /// </summary>
    public string? AssignedClientIdentifier { get; set; }

    /// <summary>
    /// 主题别名最大值。
    /// 服务器愿意在此连接中接受的主题别名数量。
    /// 0 表示服务器不接受主题别名。
    /// </summary>
    public ushort? TopicAliasMaximum { get; set; }

    /// <summary>
    /// 原因字符串。
    /// 服务器返回的人类可读的状态描述。
    /// </summary>
    public string? ReasonString { get; set; }

    /// <summary>
    /// 通配符订阅可用。
    /// true 表示服务器支持通配符订阅。
    /// false 表示服务器不支持通配符订阅。
    /// </summary>
    public bool? WildcardSubscriptionAvailable { get; set; }

    /// <summary>
    /// 订阅标识符可用。
    /// true 表示服务器支持订阅标识符。
    /// false 表示服务器不支持订阅标识符。
    /// </summary>
    public bool? SubscriptionIdentifiersAvailable { get; set; }

    /// <summary>
    /// 共享订阅可用。
    /// true 表示服务器支持共享订阅。
    /// false 表示服务器不支持共享订阅。
    /// </summary>
    public bool? SharedSubscriptionAvailable { get; set; }

    /// <summary>
    /// 服务器保活时间（秒）。
    /// 服务器要求客户端使用的保活时间。
    /// 如果设置，客户端必须使用此值而不是自己请求的值。
    /// </summary>
    public ushort? ServerKeepAlive { get; set; }

    /// <summary>
    /// 响应信息。
    /// 服务器提供的响应信息，可用于构建响应主题。
    /// </summary>
    public string? ResponseInformation { get; set; }

    /// <summary>
    /// 服务器引用。
    /// 客户端应该使用的另一个服务器地址。
    /// 用于服务器重定向。
    /// </summary>
    public string? ServerReference { get; set; }

    /// <summary>
    /// 认证方法。
    /// 服务器使用的增强认证方法。
    /// </summary>
    public string? AuthenticationMethod { get; set; }

    /// <summary>
    /// 认证数据。
    /// 与认证方法配合使用的二进制数据。
    /// </summary>
    public ReadOnlyMemory<byte> AuthenticationData { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 服务器返回的应用特定元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
