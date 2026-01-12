namespace System.Net.MQTT;

/// <summary>
/// MQTT 连接结果码。
/// </summary>
public enum MqttConnectResultCode : byte
{
    /// <summary>连接成功</summary>
    Success = 0,
    /// <summary>不支持的协议版本</summary>
    UnacceptableProtocolVersion = 1,
    /// <summary>客户端标识符被拒绝</summary>
    IdentifierRejected = 2,
    /// <summary>服务器不可用</summary>
    ServerUnavailable = 3,
    /// <summary>用户名或密码错误</summary>
    BadUsernameOrPassword = 4,
    /// <summary>未授权</summary>
    NotAuthorized = 5
}

/// <summary>
/// 表示 MQTT 连接尝试的结果。
/// </summary>
public sealed class MqttConnectResult
{
    /// <summary>
    /// 获取结果码。
    /// </summary>
    public MqttConnectResultCode ResultCode { get; init; }

    /// <summary>
    /// 获取连接是否成功。
    /// </summary>
    public bool IsSuccess => ResultCode == MqttConnectResultCode.Success;

    /// <summary>
    /// 获取服务器上是否存在会话。
    /// </summary>
    public bool SessionPresent { get; init; }

    /// <summary>
    /// 获取原因字符串（MQTT 5.0）。
    /// </summary>
    public string? ReasonString { get; init; }
}
