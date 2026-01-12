using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// AUTH 报文构建器接口（仅 MQTT 5.0）。
/// </summary>
public interface IAuthPacketBuilder : IMqttPacketBuilder<MqttAuthPacket>
{
    /// <summary>
    /// 创建 AUTH 报文。
    /// </summary>
    /// <param name="reasonCode">原因码（0x00=成功, 0x18=继续认证, 0x19=重新认证）</param>
    /// <param name="authMethod">认证方法</param>
    /// <param name="authData">认证数据</param>
    /// <returns>AUTH 报文</returns>
    MqttAuthPacket Create(byte reasonCode, string? authMethod = null, ReadOnlyMemory<byte> authData = default);
}
