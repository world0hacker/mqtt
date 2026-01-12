namespace System.Net.MQTT;

/// <summary>
/// 消息来源协议类型。
/// 定义消息是通过哪种协议发布的。
/// </summary>
public enum MqttProtocolType
{
    /// <summary>
    /// 标准 MQTT 协议 (TCP/WebSocket)。
    /// </summary>
    Mqtt,

    /// <summary>
    /// MQTT-SN 协议 (UDP)。
    /// 专为传感器网络设计的轻量级 MQTT 变体。
    /// </summary>
    MqttSn,

    /// <summary>
    /// CoAP 协议 (UDP)。
    /// 通过 CoAP-MQTT 网关桥接。
    /// </summary>
    CoAP,

    /// <summary>
    /// Broker 内部发布（如系统消息、桥接消息）。
    /// </summary>
    Internal
}
