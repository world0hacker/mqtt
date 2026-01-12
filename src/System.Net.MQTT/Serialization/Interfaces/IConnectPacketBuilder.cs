using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// CONNECT 报文构建器接口。
/// </summary>
public interface IConnectPacketBuilder : IMqttPacketBuilder<MqttConnectPacket>
{
    /// <summary>
    /// 从客户端选项创建 CONNECT 报文。
    /// </summary>
    /// <param name="options">客户端配置选项</param>
    /// <returns>CONNECT 报文</returns>
    MqttConnectPacket CreateFromOptions(MqttClientOptions options);
}
