using System.Net.MQTT.Broker.Transport;
using System.Net.MQTT.Serialization;
using System.Net.Sockets;

namespace System.Net.MQTT.Broker;

/// <summary>
/// 表示已连接的 MQTT 客户端会话。
/// </summary>
public sealed class MqttClientSession
{
    /// <summary>
    /// 获取或设置协议处理器。
    /// 根据客户端协议版本自动选择对应的处理器。
    /// </summary>
    internal IMqttProtocolHandler? ProtocolHandler { get; set; }

    /// <summary>
    /// 获取客户端标识符。
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// 获取传输连接。
    /// 统一的传输抽象，支持 TCP、UDP 等多种传输协议。
    /// </summary>
    internal ITransportConnection? Transport { get; set; }

    /// <summary>
    /// 获取传输类型。
    /// </summary>
    public TransportType TransportType => Transport?.TransportType ?? TransportType.Tcp;

    /// <summary>
    /// 获取底层 TCP 客户端（向后兼容，仅 TCP 连接有效）。
    /// </summary>
    [Obsolete("请使用 Transport 属性。此属性仅用于向后兼容。")]
    internal TcpClient? TcpClient { get; set; }

    /// <summary>
    /// 获取网络流（向后兼容，仅 TCP 连接有效）。
    /// </summary>
    [Obsolete("请使用 Transport 属性。此属性仅用于向后兼容。")]
    internal Stream? Stream { get; set; }

    /// <summary>
    /// 获取认证用户名（如果已认证）。
    /// </summary>
    public string? Username { get; internal set; }

    /// <summary>
    /// 获取是否使用清洁会话。
    /// </summary>
    public bool CleanSession { get; internal set; }

    /// <summary>
    /// 获取保活间隔（秒）。
    /// </summary>
    public ushort KeepAliveSeconds { get; internal set; }

    /// <summary>
    /// 获取遗嘱消息（如果设置）。
    /// </summary>
    public MqttApplicationMessage? WillMessage { get; internal set; }

    /// <summary>
    /// 获取已订阅的主题集合。
    /// </summary>
    public HashSet<string> Subscriptions { get; } = new();

    /// <summary>
    /// 获取订阅的 QoS 级别映射。
    /// </summary>
    public Dictionary<string, MqttQualityOfService> SubscriptionQos { get; } = new();

    /// <summary>
    /// 获取连接时间。
    /// </summary>
    public DateTime ConnectedAt { get; internal set; } = DateTime.UtcNow;

    /// <summary>
    /// 获取最后活动时间。
    /// </summary>
    public DateTime LastActivity { get; internal set; } = DateTime.UtcNow;

    /// <summary>
    /// 获取客户端是否已连接。
    /// </summary>
    public bool IsConnected { get; internal set; }

    /// <summary>
    /// 获取远程端点地址。
    /// </summary>
    public string? RemoteEndpoint { get; internal set; }

    /// <summary>
    /// 获取协议版本。
    /// </summary>
    public MqttProtocolVersion ProtocolVersion { get; internal set; }

    /// <summary>
    /// QoS > 0 的待处理消息队列。
    /// </summary>
    internal Queue<MqttApplicationMessage> PendingMessages { get; } = new();

    /// <summary>
    /// 客户端发送的 Topic Alias 映射表（Alias -> Topic）。
    /// 用于解析客户端使用 Topic Alias 发送的 PUBLISH 报文。
    /// </summary>
    internal Dictionary<ushort, string> InboundTopicAliases { get; } = new();

    /// <summary>
    /// Broker 发送给客户端的 Topic Alias 映射表（Topic -> Alias）。
    /// 用于 Broker 向客户端发送消息时使用 Topic Alias。
    /// </summary>
    internal Dictionary<string, ushort> OutboundTopicAliases { get; } = new();

    /// <summary>
    /// 客户端支持的最大 Topic Alias 数量（从 CONNECT 属性获取）。
    /// </summary>
    public ushort TopicAliasMaximum { get; internal set; }

    /// <summary>
    /// 此会话的取消令牌源。
    /// </summary>
    internal CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// 线程安全发送的锁。
    /// </summary>
    internal SemaphoreSlim SendLock { get; } = new(1, 1);

    /// <summary>
    /// 当前报文标识符。
    /// </summary>
    internal ushort PacketId { get; set; }

    /// <summary>
    /// 获取下一个报文标识符。
    /// </summary>
    /// <returns>下一个报文标识符</returns>
    internal ushort GetNextPacketId()
    {
        return ++PacketId == 0 ? ++PacketId : PacketId;
    }
}
