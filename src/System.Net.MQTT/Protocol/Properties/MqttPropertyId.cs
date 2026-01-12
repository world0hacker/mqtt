namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 属性标识符。
/// 定义了所有 MQTT 5.0 协议支持的属性类型。
/// </summary>
public enum MqttPropertyId : byte
{
    /// <summary>
    /// 载荷格式指示。
    /// 值类型：字节（0=未指定字节，1=UTF-8字符数据）
    /// 适用于：PUBLISH, Will Properties
    /// </summary>
    PayloadFormatIndicator = 0x01,

    /// <summary>
    /// 消息过期间隔（秒）。
    /// 值类型：四字节整数
    /// 适用于：PUBLISH, Will Properties
    /// </summary>
    MessageExpiryInterval = 0x02,

    /// <summary>
    /// 内容类型。
    /// 值类型：UTF-8 字符串
    /// 适用于：PUBLISH, Will Properties
    /// </summary>
    ContentType = 0x03,

    /// <summary>
    /// 响应主题。
    /// 值类型：UTF-8 字符串
    /// 适用于：PUBLISH, Will Properties
    /// </summary>
    ResponseTopic = 0x08,

    /// <summary>
    /// 相关数据。
    /// 值类型：二进制数据
    /// 适用于：PUBLISH, Will Properties
    /// </summary>
    CorrelationData = 0x09,

    /// <summary>
    /// 订阅标识符。
    /// 值类型：可变字节整数（1-268435455）
    /// 适用于：PUBLISH, SUBSCRIBE
    /// </summary>
    SubscriptionIdentifier = 0x0B,

    /// <summary>
    /// 会话过期间隔（秒）。
    /// 值类型：四字节整数
    /// 适用于：CONNECT, CONNACK, DISCONNECT
    /// </summary>
    SessionExpiryInterval = 0x11,

    /// <summary>
    /// 分配的客户端标识符。
    /// 值类型：UTF-8 字符串
    /// 适用于：CONNACK
    /// </summary>
    AssignedClientIdentifier = 0x12,

    /// <summary>
    /// 服务器保活时间（秒）。
    /// 值类型：两字节整数
    /// 适用于：CONNACK
    /// </summary>
    ServerKeepAlive = 0x13,

    /// <summary>
    /// 认证方法。
    /// 值类型：UTF-8 字符串
    /// 适用于：CONNECT, CONNACK, AUTH
    /// </summary>
    AuthenticationMethod = 0x15,

    /// <summary>
    /// 认证数据。
    /// 值类型：二进制数据
    /// 适用于：CONNECT, CONNACK, AUTH
    /// </summary>
    AuthenticationData = 0x16,

    /// <summary>
    /// 请求问题信息。
    /// 值类型：字节（0或1）
    /// 适用于：CONNECT
    /// </summary>
    RequestProblemInformation = 0x17,

    /// <summary>
    /// 遗嘱延迟间隔（秒）。
    /// 值类型：四字节整数
    /// 适用于：Will Properties
    /// </summary>
    WillDelayInterval = 0x18,

    /// <summary>
    /// 请求响应信息。
    /// 值类型：字节（0或1）
    /// 适用于：CONNECT
    /// </summary>
    RequestResponseInformation = 0x19,

    /// <summary>
    /// 响应信息。
    /// 值类型：UTF-8 字符串
    /// 适用于：CONNACK
    /// </summary>
    ResponseInformation = 0x1A,

    /// <summary>
    /// 服务器引用。
    /// 值类型：UTF-8 字符串
    /// 适用于：CONNACK, DISCONNECT
    /// </summary>
    ServerReference = 0x1C,

    /// <summary>
    /// 原因字符串。
    /// 值类型：UTF-8 字符串
    /// 适用于：CONNACK, PUBACK, PUBREC, PUBREL, PUBCOMP, SUBACK, UNSUBACK, DISCONNECT, AUTH
    /// </summary>
    ReasonString = 0x1F,

    /// <summary>
    /// 接收最大值。
    /// 值类型：两字节整数（1-65535）
    /// 适用于：CONNECT, CONNACK
    /// </summary>
    ReceiveMaximum = 0x21,

    /// <summary>
    /// 主题别名最大值。
    /// 值类型：两字节整数
    /// 适用于：CONNECT, CONNACK
    /// </summary>
    TopicAliasMaximum = 0x22,

    /// <summary>
    /// 主题别名。
    /// 值类型：两字节整数（1-65535）
    /// 适用于：PUBLISH
    /// </summary>
    TopicAlias = 0x23,

    /// <summary>
    /// 最大 QoS。
    /// 值类型：字节（0或1）
    /// 适用于：CONNACK
    /// </summary>
    MaximumQoS = 0x24,

    /// <summary>
    /// 保留消息可用。
    /// 值类型：字节（0或1）
    /// 适用于：CONNACK
    /// </summary>
    RetainAvailable = 0x25,

    /// <summary>
    /// 用户属性。
    /// 值类型：UTF-8 字符串对
    /// 适用于：所有报文类型
    /// </summary>
    UserProperty = 0x26,

    /// <summary>
    /// 最大报文大小（字节）。
    /// 值类型：四字节整数（1-268435455）
    /// 适用于：CONNECT, CONNACK
    /// </summary>
    MaximumPacketSize = 0x27,

    /// <summary>
    /// 通配符订阅可用。
    /// 值类型：字节（0或1）
    /// 适用于：CONNACK
    /// </summary>
    WildcardSubscriptionAvailable = 0x28,

    /// <summary>
    /// 订阅标识符可用。
    /// 值类型：字节（0或1）
    /// 适用于：CONNACK
    /// </summary>
    SubscriptionIdentifiersAvailable = 0x29,

    /// <summary>
    /// 共享订阅可用。
    /// 值类型：字节（0或1）
    /// 适用于：CONNACK
    /// </summary>
    SharedSubscriptionAvailable = 0x2A
}
