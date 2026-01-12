using System.Net.MQTT.Broker.Transport;

namespace System.Net.MQTT.Broker.Protocol;

/// <summary>
/// MQTT CONNECT 报文解析器接口。
/// 负责解析客户端的第一个 CONNECT 报文。
/// </summary>
public interface IMqttConnectPacketParser
{
    /// <summary>
    /// 异步解析 CONNECT 报文。
    /// </summary>
    /// <param name="connection">传输层连接。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解析结果，如果失败则返回 null。</returns>
    ValueTask<MqttConnectResult?> ParseAsync(
        ITransportConnection connection,
        CancellationToken cancellationToken);
}
