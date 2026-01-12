using System.Collections.Concurrent;
using System.Net;
using System.Net.MQTT.Broker.Transport;
using System.Net.MQTT.MqttSn.Protocol;
using System.Net.MQTT.Serialization;

namespace System.Net.MQTT.Broker.MqttSn;

/// <summary>
/// MQTT-SN 客户端会话状态。
/// </summary>
public enum MqttSnClientState
{
    /// <summary>
    /// 已断开连接。
    /// </summary>
    Disconnected,

    /// <summary>
    /// 活跃状态。
    /// </summary>
    Active,

    /// <summary>
    /// 睡眠状态。
    /// </summary>
    Asleep,

    /// <summary>
    /// 唤醒中（等待接收缓存消息）。
    /// </summary>
    Awake,

    /// <summary>
    /// 丢失连接（超时未收到心跳）。
    /// </summary>
    Lost
}

/// <summary>
/// MQTT-SN 客户端会话。
/// 管理 MQTT-SN 客户端的连接状态和消息处理。
/// </summary>
public sealed class MqttSnClientSession
{
    private readonly ConcurrentDictionary<string, ushort> _topicToId = new();
    private readonly ConcurrentDictionary<ushort, string> _idToTopic = new();
    private ushort _nextTopicId = 0x8000; // 动态主题 ID 起始

    /// <summary>
    /// 获取客户端标识符。
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// 获取传输连接。
    /// </summary>
    public ITransportConnection? Transport { get; internal set; }

    /// <summary>
    /// 获取远程端点。
    /// </summary>
    public EndPoint? RemoteEndPoint { get; internal set; }

    /// <summary>
    /// 获取或设置客户端状态。
    /// </summary>
    public MqttSnClientState State { get; internal set; } = MqttSnClientState.Disconnected;

    /// <summary>
    /// 获取或设置是否清理会话。
    /// </summary>
    public bool CleanSession { get; internal set; }

    /// <summary>
    /// 获取或设置保活间隔（秒）。
    /// </summary>
    public ushort KeepAliveSeconds { get; internal set; }

    /// <summary>
    /// 获取或设置睡眠持续时间（秒）。
    /// </summary>
    public ushort? SleepDuration { get; internal set; }

    /// <summary>
    /// 获取连接时间。
    /// </summary>
    public DateTime ConnectedAt { get; internal set; }

    /// <summary>
    /// 获取最后活动时间。
    /// </summary>
    public DateTime LastActivity { get; internal set; } = DateTime.UtcNow;

    /// <summary>
    /// 获取已订阅的主题集合。
    /// </summary>
    public HashSet<string> Subscriptions { get; } = new();

    /// <summary>
    /// 获取订阅的 QoS 级别映射。
    /// </summary>
    public Dictionary<string, MqttQualityOfService> SubscriptionQos { get; } = new();

    /// <summary>
    /// 获取遗嘱主题。
    /// </summary>
    public string? WillTopic { get; internal set; }

    /// <summary>
    /// 获取遗嘱消息。
    /// </summary>
    public byte[]? WillMessage { get; internal set; }

    /// <summary>
    /// 获取遗嘱 QoS。
    /// </summary>
    public MqttQualityOfService WillQoS { get; internal set; }

    /// <summary>
    /// 获取遗嘱保留标志。
    /// </summary>
    public bool WillRetain { get; internal set; }

    /// <summary>
    /// 睡眠客户端的消息缓冲区。
    /// </summary>
    public ConcurrentQueue<MqttApplicationMessage> SleepBuffer { get; } = new();

    /// <summary>
    /// 当前报文 ID。
    /// </summary>
    private ushort _messageId;

    /// <summary>
    /// 获取下一个消息 ID。
    /// </summary>
    public ushort GetNextMessageId()
    {
        return ++_messageId == 0 ? ++_messageId : _messageId;
    }

    /// <summary>
    /// 注册主题并获取主题 ID。
    /// </summary>
    /// <param name="topicName">主题名</param>
    /// <returns>主题 ID</returns>
    public ushort RegisterTopic(string topicName)
    {
        if (_topicToId.TryGetValue(topicName, out var existingId))
        {
            return existingId;
        }

        lock (_topicToId)
        {
            if (_topicToId.TryGetValue(topicName, out existingId))
            {
                return existingId;
            }

            var newId = _nextTopicId++;
            if (_nextTopicId > 0xFFFE)
            {
                _nextTopicId = 0x8000;
            }

            _topicToId[topicName] = newId;
            _idToTopic[newId] = topicName;
            return newId;
        }
    }

    /// <summary>
    /// 通过主题 ID 获取主题名。
    /// </summary>
    /// <param name="topicId">主题 ID</param>
    /// <returns>主题名</returns>
    public string? GetTopic(ushort topicId)
    {
        return _idToTopic.TryGetValue(topicId, out var topic) ? topic : null;
    }

    /// <summary>
    /// 通过主题名获取主题 ID。
    /// </summary>
    /// <param name="topicName">主题名</param>
    /// <returns>主题 ID</returns>
    public ushort? GetTopicId(string topicName)
    {
        return _topicToId.TryGetValue(topicName, out var id) ? id : null;
    }

    /// <summary>
    /// 更新活动时间。
    /// </summary>
    public void UpdateActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// 检查会话是否超时。
    /// </summary>
    /// <param name="tolerance">容差因子</param>
    /// <returns>是否超时</returns>
    public bool IsTimeout(double tolerance = 1.5)
    {
        if (KeepAliveSeconds == 0)
        {
            return false;
        }

        var timeout = TimeSpan.FromSeconds(KeepAliveSeconds * tolerance);
        return DateTime.UtcNow - LastActivity > timeout;
    }
}
