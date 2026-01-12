using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// DISCONNECT 报文构建器接口。
/// </summary>
public interface IDisconnectPacketBuilder : IMqttPacketBuilder<MqttDisconnectPacket>
{
    /// <summary>
    /// 创建 DISCONNECT 报文。
    /// </summary>
    /// <param name="reasonCode">原因码（MQTT 5.0）。默认 0 表示正常断开。</param>
    /// <param name="reasonString">原因字符串（MQTT 5.0）</param>
    /// <returns>DISCONNECT 报文</returns>
    MqttDisconnectPacket Create(byte reasonCode = 0, string? reasonString = null);
}
