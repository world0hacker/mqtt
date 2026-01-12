using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// CONNACK 报文构建器接口。
/// </summary>
public interface IConnAckPacketBuilder : IMqttPacketBuilder<MqttConnAckPacket>
{
    /// <summary>
    /// 创建成功的 CONNACK 报文。
    /// </summary>
    /// <param name="sessionPresent">会话是否已存在</param>
    /// <returns>CONNACK 报文</returns>
    MqttConnAckPacket CreateSuccess(bool sessionPresent);

    /// <summary>
    /// 创建失败的 CONNACK 报文。
    /// </summary>
    /// <param name="reasonCode">失败原因码</param>
    /// <param name="reasonString">原因字符串（MQTT 5.0）</param>
    /// <returns>CONNACK 报文</returns>
    MqttConnAckPacket CreateFailure(byte reasonCode, string? reasonString = null);
}
