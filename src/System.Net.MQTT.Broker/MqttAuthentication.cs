using System.Net.MQTT.Protocol;

namespace System.Net.MQTT.Broker;

/// <summary>
/// 认证上下文，包含客户端连接的完整信息。
/// </summary>
public sealed class MqttAuthenticationContext
{
    /// <summary>
    /// 获取客户端标识符。
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// 获取用户名（如果提供）。
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// 获取密码（如果提供）。
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// 获取客户端远程端点地址（如 "192.168.1.100:12345"）。
    /// </summary>
    public string? RemoteEndpoint { get; init; }

    /// <summary>
    /// 获取客户端使用的 MQTT 协议版本。
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; init; }

    /// <summary>
    /// 获取是否为清洁会话。
    /// </summary>
    public bool CleanSession { get; init; }

    /// <summary>
    /// 获取保持连接间隔（秒）。
    /// </summary>
    public ushort KeepAliveSeconds { get; init; }

    /// <summary>
    /// 获取是否有遗嘱消息。
    /// </summary>
    public bool HasWillMessage { get; init; }

    /// <summary>
    /// 获取遗嘱消息主题（如果有）。
    /// </summary>
    public string? WillTopic { get; init; }

    /// <summary>
    /// 获取遗嘱消息 QoS（如果有）。
    /// </summary>
    public MqttQualityOfService? WillQoS { get; init; }

    /// <summary>
    /// 获取是否使用 TLS 连接。
    /// </summary>
    public bool IsSecureConnection { get; init; }
}

/// <summary>
/// 认证结果。
/// </summary>
public sealed class MqttAuthenticationResult
{
    /// <summary>
    /// 获取认证是否成功。
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// 获取认证失败时的原因码（MQTT 5.0）。
    /// 0x00=成功, 0x81=格式错误, 0x82=协议错误, 0x84=不支持的协议版本,
    /// 0x85=客户端标识符无效, 0x86=用户名或密码错误, 0x87=未授权
    /// </summary>
    public byte ReasonCode { get; init; }

    /// <summary>
    /// 获取或设置认证失败时的原因描述（可选，用于日志）。
    /// </summary>
    public string? ReasonString { get; init; }

    /// <summary>
    /// 创建成功的认证结果。
    /// </summary>
    public static MqttAuthenticationResult Success { get; } = new() { IsAuthenticated = true, ReasonCode = 0x00 };

    /// <summary>
    /// 创建失败的认证结果。
    /// </summary>
    /// <param name="reasonCode">原因码</param>
    /// <param name="reasonString">原因描述</param>
    public static MqttAuthenticationResult Fail(byte reasonCode = 0x86, string? reasonString = null)
        => new() { IsAuthenticated = false, ReasonCode = reasonCode, ReasonString = reasonString };

    /// <summary>
    /// 用户名或密码错误。
    /// </summary>
    public static MqttAuthenticationResult BadCredentials { get; } = new() { IsAuthenticated = false, ReasonCode = 0x86 };

    /// <summary>
    /// 未授权。
    /// </summary>
    public static MqttAuthenticationResult NotAuthorized { get; } = new() { IsAuthenticated = false, ReasonCode = 0x87 };

    /// <summary>
    /// 客户端标识符无效。
    /// </summary>
    public static MqttAuthenticationResult InvalidClientId { get; } = new() { IsAuthenticated = false, ReasonCode = 0x85 };
}

/// <summary>
/// MQTT 客户端认证器接口。
/// </summary>
public interface IMqttAuthenticator
{
    /// <summary>
    /// 认证客户端连接。
    /// </summary>
    /// <param name="context">认证上下文，包含客户端连接的完整信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证结果</returns>
    Task<MqttAuthenticationResult> AuthenticateAsync(MqttAuthenticationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// MQTT 操作授权器接口。
/// </summary>
public interface IMqttAuthorizer
{
    /// <summary>
    /// 检查客户端是否可以发布到指定主题。
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="topic">主题名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果允许发布返回 true</returns>
    Task<bool> CanPublishAsync(MqttClientSession session, string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查客户端是否可以订阅指定主题。
    /// </summary>
    /// <param name="session">客户端会话</param>
    /// <param name="topic">主题过滤器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果允许订阅返回 true</returns>
    Task<bool> CanSubscribeAsync(MqttClientSession session, string topic, CancellationToken cancellationToken = default);
}

/// <summary>
/// 默认认证器，允许所有连接。
/// </summary>
public sealed class AllowAllAuthenticator : IMqttAuthenticator
{
    /// <summary>
    /// 单例实例。
    /// </summary>
    public static AllowAllAuthenticator Instance { get; } = new();

    /// <inheritdoc/>
    public Task<MqttAuthenticationResult> AuthenticateAsync(MqttAuthenticationContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MqttAuthenticationResult.Success);
    }
}

/// <summary>
/// 默认授权器，允许所有操作。
/// </summary>
public sealed class AllowAllAuthorizer : IMqttAuthorizer
{
    /// <summary>
    /// 单例实例。
    /// </summary>
    public static AllowAllAuthorizer Instance { get; } = new();

    /// <inheritdoc/>
    public Task<bool> CanPublishAsync(MqttClientSession session, string topic, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> CanSubscribeAsync(MqttClientSession session, string topic, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}

/// <summary>
/// 简单的内存用户名/密码认证器。
/// </summary>
public sealed class SimpleAuthenticator : IMqttAuthenticator
{
    private readonly Dictionary<string, string> _users = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 添加用户和密码。
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>当前实例（支持链式调用）</returns>
    public SimpleAuthenticator AddUser(string username, string password)
    {
        _users[username] = password;
        return this;
    }

    /// <summary>
    /// 移除用户。
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>当前实例（支持链式调用）</returns>
    public SimpleAuthenticator RemoveUser(string username)
    {
        _users.Remove(username);
        return this;
    }

    /// <inheritdoc/>
    public Task<MqttAuthenticationResult> AuthenticateAsync(MqttAuthenticationContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(context.Username))
        {
            return Task.FromResult(MqttAuthenticationResult.BadCredentials);
        }

        if (_users.TryGetValue(context.Username, out var storedPassword))
        {
            if (storedPassword == context.Password)
            {
                return Task.FromResult(MqttAuthenticationResult.Success);
            }
        }

        return Task.FromResult(MqttAuthenticationResult.BadCredentials);
    }
}
