using System.Security.Cryptography.X509Certificates;

namespace System.Net.MQTT;

/// <summary>
/// MQTT 客户端连接配置选项。
/// </summary>
public sealed class MqttClientOptions
{
    /// <summary>
    /// 获取或设置服务器主机地址。
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置服务器端口。默认值为 1883。
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// 获取或设置客户端标识符。
    /// </summary>
    public string ClientId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 获取或设置认证用户名。
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 获取或设置认证密码。
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 获取或设置是否使用 TLS/SSL 加密。
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// 获取或设置用于双向 TLS 认证的客户端证书。
    /// </summary>
    public X509Certificate2? ClientCertificate { get; set; }

    /// <summary>
    /// 获取或设置是否跳过服务器证书验证。
    /// </summary>
    public bool SkipCertificateValidation { get; set; }

    /// <summary>
    /// 获取或设置保活间隔（秒）。默认值为 60。
    /// </summary>
    public ushort KeepAliveSeconds { get; set; } = 60;

    /// <summary>
    /// 获取或设置是否以清洁会话方式启动。
    /// </summary>
    public bool CleanSession { get; set; } = true;

    /// <summary>
    /// 获取或设置连接超时时间（秒）。默认值为 30。
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置 MQTT 协议版本。默认值为 3.1.1。
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

    /// <summary>
    /// 获取或设置遗嘱消息。
    /// </summary>
    public MqttApplicationMessage? WillMessage { get; set; }

    /// <summary>
    /// 获取或设置是否在断开连接时自动重连。
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// 获取或设置重连延迟时间（毫秒）。默认值为 5000。
    /// </summary>
    public int ReconnectDelayMs { get; set; } = 5000;
}
