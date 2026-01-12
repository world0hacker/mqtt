namespace System.Net.MQTT.Protocol.Properties;

/// <summary>
/// MQTT 5.0 AUTH 报文属性。
/// 用于增强认证流程中的挑战/响应。
/// </summary>
public sealed class MqttAuthProperties
{
    /// <summary>
    /// 认证方法。
    /// 必须与 CONNECT 报文中的认证方法相同。
    /// </summary>
    public string? AuthenticationMethod { get; set; }

    /// <summary>
    /// 认证数据。
    /// 增强认证过程中传递的二进制数据。
    /// </summary>
    public ReadOnlyMemory<byte> AuthenticationData { get; set; }

    /// <summary>
    /// 原因字符串。
    /// 人类可读的状态描述。
    /// </summary>
    public string? ReasonString { get; set; }

    /// <summary>
    /// 用户属性列表。
    /// 应用特定的键值对元数据。
    /// </summary>
    public IList<MqttUserProperty> UserProperties { get; } = new List<MqttUserProperty>();
}
