using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT UNSUBACK 报文。
/// 服务器对 UNSUBSCRIBE 请求的响应。
/// </summary>
public sealed class MqttUnsubAckPacket : IMqttPacketWithId
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.UnsubAck;

    /// <inheritdoc/>
    public ushort PacketId { get; set; }

    /// <summary>
    /// 原因码列表（MQTT 5.0）。
    /// 每个元素对应 UNSUBSCRIBE 中的一个主题过滤器。
    /// MQTT 3.1.1 中此列表为空。
    /// </summary>
    public IList<byte> ReasonCodes { get; } = new List<byte>();

    /// <summary>
    /// MQTT 5.0 UNSUBACK 属性。
    /// </summary>
    public MqttUnsubAckProperties? Properties { get; set; }

    /// <summary>
    /// 判断所有取消订阅是否都成功。
    /// </summary>
    public bool AllSuccess
    {
        get
        {
            if (ReasonCodes.Count == 0) return true; // MQTT 3.1.1
            foreach (var code in ReasonCodes)
            {
                if (code >= 0x80) return false;
            }
            return true;
        }
    }
}
