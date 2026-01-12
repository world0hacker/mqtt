namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 DISCONNECT 报文属性。
/// </summary>
public sealed class MqttDisconnectProperties
{
    /// <summary>
    /// 会话过期间隔（秒）。
    /// 用于在断开连接时修改会话过期间隔。
    /// 只有客户端可以设置此属性。
    /// </summary>
    public uint? SessionExpiryInterval { get; set; }

    /// <summary>
    /// 原因字符串。
    /// 人类可读的断开原因描述。
    /// </summary>
    public string? ReasonString { get; set; }

    /// <summary>
    /// 服务器引用。
    /// 客户端应该连接的另一个服务器地址。
    /// 仅由服务器设置，用于服务器重定向。
    /// </summary>
    public string? ServerReference { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 应用特定的键值对元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
