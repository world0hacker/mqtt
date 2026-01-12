namespace System.Net.MQTT.MqttSn.Protocol;

/// <summary>
/// MQTT-SN 报文类型枚举。
/// 基于 MQTT-SN 1.2 规范定义。
/// </summary>
public enum MqttSnPacketType : byte
{
    /// <summary>
    /// 网关广播报文。网关定期发送以宣告自己的存在。
    /// </summary>
    Advertise = 0x00,

    /// <summary>
    /// 搜索网关请求。客户端发送以查找可用网关。
    /// </summary>
    SearchGw = 0x01,

    /// <summary>
    /// 网关信息响应。响应 SearchGw 请求。
    /// </summary>
    GwInfo = 0x02,

    /// <summary>
    /// 保留。
    /// </summary>
    Reserved03 = 0x03,

    /// <summary>
    /// 连接请求。客户端发送以建立连接。
    /// </summary>
    Connect = 0x04,

    /// <summary>
    /// 连接确认。网关响应 Connect 请求。
    /// </summary>
    ConnAck = 0x05,

    /// <summary>
    /// 遗嘱主题设置。客户端发送以设置遗嘱消息主题。
    /// </summary>
    WillTopicReq = 0x06,

    /// <summary>
    /// 遗嘱主题。客户端响应 WillTopicReq。
    /// </summary>
    WillTopic = 0x07,

    /// <summary>
    /// 遗嘱消息请求。网关请求遗嘱消息内容。
    /// </summary>
    WillMsgReq = 0x08,

    /// <summary>
    /// 遗嘱消息。客户端响应 WillMsgReq。
    /// </summary>
    WillMsg = 0x09,

    /// <summary>
    /// 主题注册请求。客户端/网关发送以注册主题并获取主题 ID。
    /// </summary>
    Register = 0x0A,

    /// <summary>
    /// 主题注册确认。响应 Register 请求。
    /// </summary>
    RegAck = 0x0B,

    /// <summary>
    /// 发布消息。
    /// </summary>
    Publish = 0x0C,

    /// <summary>
    /// 发布确认（QoS 1）。
    /// </summary>
    PubAck = 0x0D,

    /// <summary>
    /// 发布完成（QoS 2 第一步）。
    /// </summary>
    PubComp = 0x0E,

    /// <summary>
    /// 发布收到（QoS 2 第二步）。
    /// </summary>
    PubRec = 0x0F,

    /// <summary>
    /// 发布释放（QoS 2 第三步）。
    /// </summary>
    PubRel = 0x10,

    /// <summary>
    /// 保留。
    /// </summary>
    Reserved11 = 0x11,

    /// <summary>
    /// 订阅请求。
    /// </summary>
    Subscribe = 0x12,

    /// <summary>
    /// 订阅确认。
    /// </summary>
    SubAck = 0x13,

    /// <summary>
    /// 取消订阅请求。
    /// </summary>
    Unsubscribe = 0x14,

    /// <summary>
    /// 取消订阅确认。
    /// </summary>
    UnsubAck = 0x15,

    /// <summary>
    /// 心跳请求。
    /// </summary>
    PingReq = 0x16,

    /// <summary>
    /// 心跳响应。
    /// </summary>
    PingResp = 0x17,

    /// <summary>
    /// 断开连接。
    /// </summary>
    Disconnect = 0x18,

    /// <summary>
    /// 保留。
    /// </summary>
    Reserved19 = 0x19,

    /// <summary>
    /// 遗嘱主题更新。
    /// </summary>
    WillTopicUpd = 0x1A,

    /// <summary>
    /// 遗嘱主题更新响应。
    /// </summary>
    WillTopicResp = 0x1B,

    /// <summary>
    /// 遗嘱消息更新。
    /// </summary>
    WillMsgUpd = 0x1C,

    /// <summary>
    /// 遗嘱消息更新响应。
    /// </summary>
    WillMsgResp = 0x1D,

    /// <summary>
    /// 封装消息。用于转发器。
    /// </summary>
    Encapsulated = 0xFE
}
