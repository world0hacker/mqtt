namespace System.Net.MQTT.Broker.Transport;

/// <summary>
/// 传输层类型枚举。
/// 定义支持的传输协议类型。
/// </summary>
public enum TransportType
{
    /// <summary>
    /// TCP 传输（标准 MQTT）。
    /// 面向连接，可靠传输。
    /// </summary>
    Tcp,

    /// <summary>
    /// TCP + TLS 传输（加密 MQTT）。
    /// 面向连接，可靠传输，支持 TLS 加密。
    /// </summary>
    TcpTls,

    /// <summary>
    /// UDP 传输（用于 MQTT-SN 和 CoAP）。
    /// 无连接，不可靠传输，需要应用层处理可靠性。
    /// </summary>
    Udp,

    /// <summary>
    /// WebSocket 传输。
    /// 基于 HTTP 升级的双向通信。
    /// </summary>
    WebSocket
}
