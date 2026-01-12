namespace System.Net.MQTT.Broker;

/// <summary>
/// 客户端连接事件参数。
/// </summary>
public sealed class MqttClientConnectedEventArgs : EventArgs
{
    /// <summary>
    /// 获取客户端会话。
    /// </summary>
    public MqttClientSession Session { get; init; } = null!;
}

/// <summary>
/// 客户端断开连接事件参数。
/// </summary>
public sealed class MqttClientDisconnectedEventArgs : EventArgs
{
    /// <summary>
    /// 获取客户端会话。
    /// </summary>
    public MqttClientSession Session { get; init; } = null!;

    /// <summary>
    /// 获取断开连接时的异常（如果有）。
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// 获取是否为正常断开。
    /// </summary>
    public bool Graceful { get; init; }
}

/// <summary>
/// 单个订阅请求项。
/// </summary>
public sealed class MqttSubscriptionRequest
{
    /// <summary>
    /// 获取主题过滤器。
    /// </summary>
    public string TopicFilter { get; init; } = null!;

    /// <summary>
    /// 获取请求的 QoS 级别。
    /// </summary>
    public MqttQualityOfService RequestedQoS { get; init; }

    /// <summary>
    /// 获取或设置是否接受此订阅。默认为 true。
    /// </summary>
    public bool Accept { get; set; } = true;

    /// <summary>
    /// 获取或设置授予的 QoS 级别。默认与请求的 QoS 相同。
    /// 仅在 Accept = true 时有效。
    /// </summary>
    public MqttQualityOfService GrantedQoS { get; set; }

    /// <summary>
    /// 获取或设置拒绝原因码（仅在 Accept = false 时使用）。
    /// MQTT 5.0: 0x80=未指定错误, 0x83=特定于实现, 0x87=未授权, 0x8F=主题过滤器无效, 0x91=包标识符已使用, 0x97=超出配额, 0x9E=不支持共享订阅, 0xA1=不支持订阅标识符, 0xA2=不支持通配符订阅
    /// MQTT 3.1.1: 0x80=失败
    /// </summary>
    public byte RejectReasonCode { get; set; } = 0x80;
}

/// <summary>
/// 客户端订阅事件参数（订阅处理前触发）。
/// 允许用户控制是否接受每个订阅请求。
/// </summary>
public sealed class MqttClientSubscribingEventArgs : EventArgs
{
    /// <summary>
    /// 获取客户端会话。
    /// </summary>
    public MqttClientSession Session { get; init; } = null!;

    /// <summary>
    /// 获取订阅请求列表。可修改每个请求的 Accept 和 GrantedQoS 属性。
    /// </summary>
    public IList<MqttSubscriptionRequest> Subscriptions { get; init; } = null!;
}

/// <summary>
/// 客户端订阅完成事件参数（订阅处理后触发）。
/// </summary>
public sealed class MqttClientSubscribedEventArgs : EventArgs
{
    /// <summary>
    /// 获取客户端会话。
    /// </summary>
    public MqttClientSession Session { get; init; } = null!;

    /// <summary>
    /// 获取成功订阅的主题列表。
    /// </summary>
    public IReadOnlyList<MqttTopicSubscription> Subscriptions { get; init; } = Array.Empty<MqttTopicSubscription>();
}

/// <summary>
/// 单个取消订阅请求项。
/// </summary>
public sealed class MqttUnsubscribeRequest
{
    /// <summary>
    /// 获取主题过滤器。
    /// </summary>
    public string TopicFilter { get; init; } = null!;

    /// <summary>
    /// 获取或设置是否接受此取消订阅。默认为 true。
    /// </summary>
    public bool Accept { get; set; } = true;

    /// <summary>
    /// 获取或设置拒绝原因码（仅在 Accept = false 时使用）。
    /// MQTT 5.0: 0x11=未找到订阅, 0x80=未指定错误, 0x83=特定于实现, 0x87=未授权
    /// </summary>
    public byte RejectReasonCode { get; set; } = 0x80;
}

/// <summary>
/// 客户端取消订阅事件参数（取消订阅处理前触发）。
/// 允许用户控制是否接受每个取消订阅请求。
/// </summary>
public sealed class MqttClientUnsubscribingEventArgs : EventArgs
{
    /// <summary>
    /// 获取客户端会话。
    /// </summary>
    public MqttClientSession Session { get; init; } = null!;

    /// <summary>
    /// 获取取消订阅请求列表。可修改每个请求的 Accept 属性。
    /// </summary>
    public IList<MqttUnsubscribeRequest> Unsubscriptions { get; init; } = null!;
}

/// <summary>
/// 客户端取消订阅完成事件参数（取消订阅处理后触发）。
/// </summary>
public sealed class MqttClientUnsubscribedEventArgs : EventArgs
{
    /// <summary>
    /// 获取客户端会话。
    /// </summary>
    public MqttClientSession Session { get; init; } = null!;

    /// <summary>
    /// 获取成功取消订阅的主题列表。
    /// </summary>
    public IReadOnlyList<string> Topics { get; init; } = Array.Empty<string>();
}

/// <summary>
/// 消息发布事件参数（消息处理前触发）。
/// 允许用户控制是否处理和转发消息。
/// </summary>
public sealed class MqttMessagePublishingEventArgs : EventArgs
{
    /// <summary>
    /// 获取发布消息的客户端会话。
    /// 对于 MQTT-SN 和 CoAP 协议，此值可能为 null。
    /// </summary>
    public MqttClientSession? Session { get; init; }

    /// <summary>
    /// 获取消息来源的协议类型。
    /// </summary>
    public MqttProtocolType Protocol { get; init; } = MqttProtocolType.Mqtt;

    /// <summary>
    /// 获取消息来源的客户端标识。
    /// 对于 MQTT 是 ClientId，对于 MQTT-SN/CoAP 是远程端点标识。
    /// </summary>
    public string? SourceClientId { get; init; }

    /// <summary>
    /// 获取发布的消息。
    /// </summary>
    public MqttApplicationMessage Message { get; init; } = null!;

    /// <summary>
    /// 获取或设置是否处理此消息（存储保留消息、转发给订阅者）。
    /// 设为 false 可丢弃消息。默认为 true。
    /// </summary>
    public bool ProcessMessage { get; set; } = true;

    /// <summary>
    /// 获取或设置是否发送 ACK 响应（用于 QoS > 0）。
    /// 即使 ProcessMessage = false，也可能需要发送 ACK。默认为 true。
    /// </summary>
    public bool SendAck { get; set; } = true;

    /// <summary>
    /// 获取或设置 PUBACK/PUBREC 原因码（仅 MQTT 5.0）。
    /// 0x00=成功, 0x10=无匹配订阅者, 0x80=未指定错误, 0x83=特定于实现, 0x87=未授权, 0x90=主题名无效, 0x91=包标识符已使用, 0x97=超出配额
    /// </summary>
    public byte ReasonCode { get; set; } = 0x00;
}

/// <summary>
/// 消息发布完成事件参数（消息处理后触发）。
/// </summary>
public sealed class MqttMessagePublishedEventArgs : EventArgs
{
    /// <summary>
    /// 获取发布消息的客户端会话。
    /// 对于 MQTT-SN 和 CoAP 协议，此值可能为 null。
    /// </summary>
    public MqttClientSession? Session { get; init; }

    /// <summary>
    /// 获取消息来源的协议类型。
    /// </summary>
    public MqttProtocolType Protocol { get; init; } = MqttProtocolType.Mqtt;

    /// <summary>
    /// 获取消息来源的客户端标识。
    /// 对于 MQTT 是 ClientId，对于 MQTT-SN/CoAP 是远程端点标识。
    /// </summary>
    public string? SourceClientId { get; init; }

    /// <summary>
    /// 获取发布的消息。
    /// </summary>
    public MqttApplicationMessage Message { get; init; } = null!;

    /// <summary>
    /// 获取接收到此消息的客户端数量。
    /// </summary>
    public int DeliveredCount { get; init; }
}

/// <summary>
/// 消息无接收者事件参数（消息没有匹配的订阅者时触发）。
/// </summary>
public sealed class MqttMessageNotDeliveredEventArgs : EventArgs
{
    /// <summary>
    /// 获取发布消息的客户端会话。
    /// 对于 MQTT-SN 和 CoAP 协议，此值可能为 null。
    /// </summary>
    public MqttClientSession? Session { get; init; }

    /// <summary>
    /// 获取消息来源的协议类型。
    /// </summary>
    public MqttProtocolType Protocol { get; init; } = MqttProtocolType.Mqtt;

    /// <summary>
    /// 获取消息来源的客户端标识。
    /// </summary>
    public string? SourceClientId { get; init; }

    /// <summary>
    /// 获取未被投递的消息。
    /// </summary>
    public MqttApplicationMessage Message { get; init; } = null!;
}

/// <summary>
/// 消息已投递事件参数（消息成功发送给订阅者时触发）。
/// </summary>
public sealed class MqttMessageDeliveredEventArgs : EventArgs
{
    /// <summary>
    /// 获取发布消息的原始客户端会话（可能为 null，如服务端主动发送或来自 MQTT-SN/CoAP）。
    /// </summary>
    public MqttClientSession? SourceSession { get; init; }

    /// <summary>
    /// 获取消息来源的协议类型。
    /// </summary>
    public MqttProtocolType Protocol { get; init; } = MqttProtocolType.Mqtt;

    /// <summary>
    /// 获取消息来源的客户端标识。
    /// </summary>
    public string? SourceClientId { get; init; }

    /// <summary>
    /// 获取接收消息的客户端会话。
    /// </summary>
    public MqttClientSession TargetSession { get; init; } = null!;

    /// <summary>
    /// 获取投递的消息。
    /// </summary>
    public MqttApplicationMessage Message { get; init; } = null!;
}
