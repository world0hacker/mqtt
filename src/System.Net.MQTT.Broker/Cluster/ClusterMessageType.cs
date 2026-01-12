namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// 集群内部消息类型。
/// </summary>
public enum ClusterMessageType : byte
{
    /// <summary>
    /// 心跳消息。
    /// </summary>
    Heartbeat = 0x01,

    /// <summary>
    /// 握手请求。
    /// </summary>
    HandshakeRequest = 0x02,

    /// <summary>
    /// 握手响应。
    /// </summary>
    HandshakeResponse = 0x03,

    /// <summary>
    /// 发布消息（转发 MQTT PUBLISH）。
    /// </summary>
    Publish = 0x10,

    /// <summary>
    /// 订阅同步。
    /// </summary>
    Subscribe = 0x20,

    /// <summary>
    /// 取消订阅同步。
    /// </summary>
    Unsubscribe = 0x21,

    /// <summary>
    /// 节点离开通知。
    /// </summary>
    NodeLeave = 0x30,

    /// <summary>
    /// 节点发现请求。
    /// </summary>
    DiscoverRequest = 0x40,

    /// <summary>
    /// 节点发现响应。
    /// </summary>
    DiscoverResponse = 0x41,

    /// <summary>
    /// 保留消息同步请求。
    /// </summary>
    RetainedSyncRequest = 0x50,

    /// <summary>
    /// 保留消息同步数据。
    /// </summary>
    RetainedSyncData = 0x51
}
