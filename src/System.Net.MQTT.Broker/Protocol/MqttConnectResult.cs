namespace System.Net.MQTT.Broker.Protocol;

/// <summary>
/// MQTT CONNECT 报文解析结果。
/// </summary>
public sealed class MqttConnectResult
{
    /// <summary>
    /// 获取协议版本。
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; init; }

    /// <summary>
    /// 获取客户端 ID。
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// 获取是否清理会话。
    /// </summary>
    public bool CleanSession { get; init; }

    /// <summary>
    /// 获取保活时间（秒）。
    /// </summary>
    public ushort KeepAlive { get; init; }

    /// <summary>
    /// 获取遗嘱消息。
    /// </summary>
    public MqttApplicationMessage? WillMessage { get; init; }

    /// <summary>
    /// 获取用户名。
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// 获取密码。
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// 获取 MQTT 5.0 连接属性。
    /// </summary>
    public MqttConnectProperties? Properties { get; init; }
}

/// <summary>
/// MQTT 5.0 连接属性。
/// </summary>
public sealed class MqttConnectProperties
{
    /// <summary>
    /// 获取或设置会话过期间隔（秒）。
    /// </summary>
    public uint? SessionExpiryInterval { get; set; }

    /// <summary>
    /// 获取或设置接收最大值。
    /// </summary>
    public ushort? ReceiveMaximum { get; set; }

    /// <summary>
    /// 获取或设置最大报文大小。
    /// </summary>
    public uint? MaximumPacketSize { get; set; }

    /// <summary>
    /// 获取或设置主题别名最大值。
    /// </summary>
    public ushort? TopicAliasMaximum { get; set; }

    /// <summary>
    /// 获取或设置是否请求响应信息。
    /// </summary>
    public bool? RequestResponseInformation { get; set; }

    /// <summary>
    /// 获取或设置是否请求问题信息。
    /// </summary>
    public bool? RequestProblemInformation { get; set; }

    /// <summary>
    /// 获取或设置认证方法。
    /// </summary>
    public string? AuthenticationMethod { get; set; }

    /// <summary>
    /// 获取或设置认证数据。
    /// </summary>
    public byte[]? AuthenticationData { get; set; }
}
