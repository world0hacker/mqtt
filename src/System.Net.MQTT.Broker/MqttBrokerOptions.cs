using System.Security.Cryptography.X509Certificates;

namespace System.Net.MQTT.Broker;

/// <summary>
/// MQTT Broker 配置选项。
/// </summary>
public sealed class MqttBrokerOptions
{
    /// <summary>
    /// 获取或设置绑定的 IP 地址。默认为任意地址。
    /// </summary>
    public string BindAddress { get; set; } = "0.0.0.0";

    /// <summary>
    /// 获取或设置监听端口。默认为 1883。
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// 获取或设置是否启用 TLS/SSL。
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// 获取或设置 TLS 端口。默认为 8883。
    /// </summary>
    public int TlsPort { get; set; } = 8883;

    /// <summary>
    /// 获取或设置 TLS 使用的服务器证书。
    /// </summary>
    public X509Certificate2? ServerCertificate { get; set; }

    /// <summary>
    /// 获取或设置是否要求客户端证书。
    /// </summary>
    public bool RequireClientCertificate { get; set; }

    /// <summary>
    /// 获取或设置最大并发连接数。
    /// </summary>
    public int MaxConnections { get; set; } = 10000;

    /// <summary>
    /// 获取或设置连接超时时间（秒）。默认为 30。
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 获取或设置保活容差因子。默认为 1.5。
    /// </summary>
    public double KeepAliveTolerance { get; set; } = 1.5;

    /// <summary>
    /// 获取或设置最大消息大小（字节）。默认为 5MB。
    /// </summary>
    public int MaxMessageSize { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// 获取或设置是否允许匿名连接。
    /// </summary>
    public bool AllowAnonymous { get; set; } = true;

    /// <summary>
    /// 获取或设置是否启用保留消息。
    /// </summary>
    public bool EnableRetainedMessages { get; set; } = true;

    /// <summary>
    /// 获取或设置是否启用持久会话。
    /// </summary>
    public bool EnablePersistentSessions { get; set; } = true;

    /// <summary>
    /// 获取或设置每个客户端的最大离线消息数量。
    /// 当离线消息超过此数量时，最旧的消息将被丢弃。
    /// </summary>
    public int MaxOfflineMessagesPerClient { get; set; } = int.MaxValue;

    /// <summary>
    /// 获取或设置是否在客户端连接时自动投递离线消息。
    /// </summary>
    public bool AutoDeliverOfflineMessages { get; set; } = true;

    #region MQTT-SN 配置

    /// <summary>
    /// 获取或设置是否启用 MQTT-SN 网关。
    /// MQTT-SN 是专为传感器网络设计的 MQTT 变体，基于 UDP。
    /// </summary>
    public bool EnableMqttSn { get; set; } = false;

    /// <summary>
    /// 获取或设置 MQTT-SN 监听端口。默认为 1885。
    /// </summary>
    public int MqttSnPort { get; set; } = 1885;

    /// <summary>
    /// 获取或设置网关广播间隔（秒）。
    /// 网关定期发送 ADVERTISE 报文。默认为 900 秒（15 分钟）。
    /// </summary>
    public int GatewayAdvertiseIntervalSeconds { get; set; } = 900;

    /// <summary>
    /// 获取或设置睡眠客户端消息缓冲区大小。
    /// 为睡眠中的客户端缓存的消息数量上限。默认为 100。
    /// </summary>
    public int SleepingClientBufferSize { get; set; } = 100;

    /// <summary>
    /// 获取或设置 MQTT-SN 客户端会话超时时间（秒）。
    /// UDP 虚拟连接在此时间内无活动将被清理。默认为 1800 秒（30 分钟）。
    /// </summary>
    public int MqttSnSessionTimeoutSeconds { get; set; } = 1800;

    #endregion

    #region CoAP 配置

    /// <summary>
    /// 获取或设置是否启用 CoAP 网关。
    /// CoAP 是受限设备的轻量级 Web 协议，基于 UDP。
    /// </summary>
    public bool EnableCoAP { get; set; } = false;

    /// <summary>
    /// 获取或设置 CoAP 监听端口。默认为 5683（CoAP 标准端口）。
    /// </summary>
    public int CoapPort { get; set; } = 5683;

    /// <summary>
    /// 获取或设置 CoAP 资源路径前缀。
    /// CoAP URI: coap://broker/{prefix}/topic → MQTT Topic: topic
    /// 默认为 "mqtt"。
    /// </summary>
    public string CoapMqttPrefix { get; set; } = "mqtt";

    /// <summary>
    /// 获取或设置 CoAP 最大重传次数。
    /// 对于 CON（可确认）消息的最大重传尝试次数。默认为 4。
    /// </summary>
    public int CoapMaxRetransmit { get; set; } = 4;

    /// <summary>
    /// 获取或设置 CoAP ACK 超时时间（毫秒）。
    /// 等待 ACK 响应的初始超时时间，后续使用指数退避。默认为 2000ms。
    /// </summary>
    public int CoapAckTimeoutMs { get; set; } = 2000;

    /// <summary>
    /// 获取或设置 CoAP 最大块大小。
    /// 用于块传输的最大块大小。默认为 1024 字节。
    /// </summary>
    public int CoapMaxBlockSize { get; set; } = 1024;

    /// <summary>
    /// 获取或设置 CoAP 客户端会话超时时间（秒）。
    /// UDP 虚拟连接在此时间内无活动将被清理。默认为 1800 秒（30 分钟）。
    /// </summary>
    public int CoapSessionTimeoutSeconds { get; set; } = 1800;

    #endregion
}
