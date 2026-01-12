namespace System.Net.MQTT;

/// <summary>
/// MQTT 报文类型，按照 MQTT 规范定义。
/// </summary>
public enum MqttPacketType : byte
{
    /// <summary>连接请求</summary>
    Connect = 1,
    /// <summary>连接确认</summary>
    ConnAck = 2,
    /// <summary>发布消息</summary>
    Publish = 3,
    /// <summary>发布确认（QoS 1）</summary>
    PubAck = 4,
    /// <summary>发布已接收（QoS 2 第一步）</summary>
    PubRec = 5,
    /// <summary>发布释放（QoS 2 第二步）</summary>
    PubRel = 6,
    /// <summary>发布完成（QoS 2 第三步）</summary>
    PubComp = 7,
    /// <summary>订阅请求</summary>
    Subscribe = 8,
    /// <summary>订阅确认</summary>
    SubAck = 9,
    /// <summary>取消订阅请求</summary>
    Unsubscribe = 10,
    /// <summary>取消订阅确认</summary>
    UnsubAck = 11,
    /// <summary>心跳请求</summary>
    PingReq = 12,
    /// <summary>心跳响应</summary>
    PingResp = 13,
    /// <summary>断开连接</summary>
    Disconnect = 14,
    /// <summary>认证交换（MQTT 5.0）</summary>
    Auth = 15
}
