namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// 集群内部消息。
/// </summary>
public sealed class ClusterMessage
{
    /// <summary>
    /// 获取或设置消息类型。
    /// </summary>
    public ClusterMessageType Type { get; set; }

    /// <summary>
    /// 获取或设置消息来源节点 ID。
    /// </summary>
    public string SourceNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置消息唯一标识（用于去重）。
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置主题（用于 Subscribe/Unsubscribe/Publish）。
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// 获取或设置消息载荷。
    /// </summary>
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 获取或设置 QoS 级别（用于 Publish）。
    /// </summary>
    public MqttQualityOfService QoS { get; set; }

    /// <summary>
    /// 获取或设置保留标志（用于 Publish）。
    /// </summary>
    public bool Retain { get; set; }

    /// <summary>
    /// 获取或设置时间戳（用于消息排序和过期检测）。
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// 创建心跳消息。
    /// </summary>
    /// <param name="nodeId">节点 ID</param>
    /// <returns>心跳消息</returns>
    public static ClusterMessage CreateHeartbeat(string nodeId)
    {
        return new ClusterMessage
        {
            Type = ClusterMessageType.Heartbeat,
            SourceNodeId = nodeId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// 创建发布消息。
    /// </summary>
    /// <param name="nodeId">来源节点 ID</param>
    /// <param name="message">MQTT 应用消息</param>
    /// <param name="messageId">消息唯一标识</param>
    /// <returns>发布消息</returns>
    public static ClusterMessage CreatePublish(string nodeId, MqttApplicationMessage message, string messageId)
    {
        return new ClusterMessage
        {
            Type = ClusterMessageType.Publish,
            SourceNodeId = nodeId,
            MessageId = messageId,
            Topic = message.Topic,
            Payload = message.Payload.ToArray(),
            QoS = message.QualityOfService,
            Retain = message.Retain,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// 创建订阅同步消息。
    /// </summary>
    /// <param name="nodeId">节点 ID</param>
    /// <param name="topic">主题过滤器</param>
    /// <returns>订阅同步消息</returns>
    public static ClusterMessage CreateSubscribe(string nodeId, string topic)
    {
        return new ClusterMessage
        {
            Type = ClusterMessageType.Subscribe,
            SourceNodeId = nodeId,
            Topic = topic,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// 创建取消订阅同步消息。
    /// </summary>
    /// <param name="nodeId">节点 ID</param>
    /// <param name="topic">主题过滤器</param>
    /// <returns>取消订阅同步消息</returns>
    public static ClusterMessage CreateUnsubscribe(string nodeId, string topic)
    {
        return new ClusterMessage
        {
            Type = ClusterMessageType.Unsubscribe,
            SourceNodeId = nodeId,
            Topic = topic,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// 创建保留消息同步请求。
    /// </summary>
    /// <param name="nodeId">请求节点 ID</param>
    /// <returns>保留消息同步请求</returns>
    public static ClusterMessage CreateRetainedSyncRequest(string nodeId)
    {
        return new ClusterMessage
        {
            Type = ClusterMessageType.RetainedSyncRequest,
            SourceNodeId = nodeId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// 创建保留消息同步数据。
    /// </summary>
    /// <param name="nodeId">来源节点 ID</param>
    /// <param name="messages">保留消息列表</param>
    /// <returns>保留消息同步数据</returns>
    public static ClusterMessage CreateRetainedSyncData(string nodeId, IEnumerable<MqttApplicationMessage> messages)
    {
        // 将多个保留消息序列化到 Payload 中
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var messageList = messages.ToList();
        writer.Write(messageList.Count);

        foreach (var msg in messageList)
        {
            // 写入主题
            var topicBytes = System.Text.Encoding.UTF8.GetBytes(msg.Topic);
            writer.Write((ushort)topicBytes.Length);
            writer.Write(topicBytes);

            // 写入 QoS 和 Retain 标志
            writer.Write((byte)((int)msg.QualityOfService | (msg.Retain ? 0x04 : 0)));

            // 写入 Payload
            writer.Write(msg.Payload.Length);
            writer.Write(msg.Payload.ToArray());
        }

        return new ClusterMessage
        {
            Type = ClusterMessageType.RetainedSyncData,
            SourceNodeId = nodeId,
            Payload = ms.ToArray(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    /// 解析保留消息同步数据中的消息列表。
    /// </summary>
    /// <returns>保留消息列表</returns>
    public List<MqttApplicationMessage> ParseRetainedMessages()
    {
        if (Type != ClusterMessageType.RetainedSyncData || Payload.Length == 0)
            return new List<MqttApplicationMessage>();

        var messages = new List<MqttApplicationMessage>();
        using var ms = new MemoryStream(Payload);
        using var reader = new BinaryReader(ms);

        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            // 读取主题
            var topicLength = reader.ReadUInt16();
            var topicBytes = reader.ReadBytes(topicLength);
            var topic = System.Text.Encoding.UTF8.GetString(topicBytes);

            // 读取 QoS 和 Retain 标志
            var flags = reader.ReadByte();
            var qos = (MqttQualityOfService)(flags & 0x03);
            var retain = (flags & 0x04) != 0;

            // 读取 Payload
            var payloadLength = reader.ReadInt32();
            var payload = reader.ReadBytes(payloadLength);

            messages.Add(new MqttApplicationMessage
            {
                Topic = topic,
                Payload = payload,
                QualityOfService = qos,
                Retain = retain
            });
        }

        return messages;
    }
}
