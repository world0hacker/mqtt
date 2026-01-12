using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT 5.0 AUTH 报文。
/// 用于增强认证流程中的挑战/响应。
/// 仅在 MQTT 5.0 中支持。
/// </summary>
public sealed class MqttAuthPacket : IMqttPacket
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.Auth;

    /// <summary>
    /// 原因码。
    /// 0x00 = 成功
    /// 0x18 = 继续认证
    /// 0x19 = 重新认证
    /// </summary>
    public byte ReasonCode { get; set; }

    /// <summary>
    /// AUTH 属性。
    /// </summary>
    public MqttAuthProperties? Properties { get; set; }

    /// <summary>
    /// 判断是否需要继续认证。
    /// </summary>
    public bool IsContinueAuthentication => ReasonCode == 0x18;

    /// <summary>
    /// 判断是否为重新认证。
    /// </summary>
    public bool IsReAuthenticate => ReasonCode == 0x19;

    /// <summary>
    /// 判断认证是否成功。
    /// </summary>
    public bool IsSuccess => ReasonCode == 0x00;
}
