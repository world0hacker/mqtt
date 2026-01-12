namespace System.Net.MQTT.CoAP.Protocol;

/// <summary>
/// CoAP 消息类型枚举。
/// </summary>
public enum CoapMessageType : byte
{
    /// <summary>
    /// 可确认消息 (Confirmable)。
    /// 需要接收方回复 ACK 或 RST。
    /// </summary>
    Confirmable = 0,

    /// <summary>
    /// 不可确认消息 (Non-Confirmable)。
    /// 不需要接收方确认，但接收方可发送 RST 表示错误。
    /// </summary>
    NonConfirmable = 1,

    /// <summary>
    /// 确认消息 (Acknowledgement)。
    /// 对 CON 消息的确认响应。
    /// </summary>
    Acknowledgement = 2,

    /// <summary>
    /// 重置消息 (Reset)。
    /// 表示收到无法处理的消息。
    /// </summary>
    Reset = 3
}
