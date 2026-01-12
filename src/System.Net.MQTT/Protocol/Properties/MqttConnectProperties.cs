namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 CONNECT 报文属性。
/// 客户端在连接时发送给服务器的属性集合。
/// </summary>
public sealed class MqttConnectProperties
{
    /// <summary>
    /// 会话过期间隔（秒）。
    /// 0 表示会话在网络连接关闭时结束。
    /// 0xFFFFFFFF 表示会话永不过期。
    /// null 表示未设置（使用服务器默认值）。
    /// </summary>
    public uint? SessionExpiryInterval { get; set; }

    /// <summary>
    /// 接收最大值。
    /// 客户端愿意同时处理的未确认 QoS 1 和 QoS 2 PUBLISH 报文数量。
    /// 范围：1-65535，默认值为 65535。
    /// </summary>
    public ushort? ReceiveMaximum { get; set; }

    /// <summary>
    /// 最大报文大小（字节）。
    /// 客户端愿意接收的最大报文大小。
    /// 范围：1-268435455。
    /// null 表示无限制。
    /// </summary>
    public uint? MaximumPacketSize { get; set; }

    /// <summary>
    /// 主题别名最大值。
    /// 客户端愿意在此连接中接受的主题别名数量。
    /// 0 表示不接受主题别名。
    /// </summary>
    public ushort? TopicAliasMaximum { get; set; }

    /// <summary>
    /// 请求响应信息。
    /// true 表示客户端希望服务器在 CONNACK 中返回响应信息。
    /// </summary>
    public bool? RequestResponseInformation { get; set; }

    /// <summary>
    /// 请求问题信息。
    /// true 表示客户端希望服务器在失败时返回原因字符串和用户属性。
    /// 默认为 true。
    /// </summary>
    public bool? RequestProblemInformation { get; set; }

    /// <summary>
    /// 认证方法。
    /// 用于增强认证的方法名称。
    /// </summary>
    public string? AuthenticationMethod { get; set; }

    /// <summary>
    /// 认证数据。
    /// 与认证方法配合使用的二进制数据。
    /// </summary>
    public ReadOnlyMemory<byte> AuthenticationData { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 可以包含任意键值对，用于传递应用特定的元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
