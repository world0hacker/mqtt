using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT SUBACK 报文。
/// 服务器对 SUBSCRIBE 请求的响应。
/// </summary>
public sealed class MqttSubAckPacket : IMqttPacketWithId
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.SubAck;

    /// <inheritdoc/>
    public ushort PacketId { get; set; }

    /// <summary>
    /// 订阅结果码列表。
    /// 每个元素对应 SUBSCRIBE 中的一个主题过滤器。
    /// MQTT 3.1.1: 0x00=QoS0, 0x01=QoS1, 0x02=QoS2, 0x80=失败
    /// MQTT 5.0: 使用 MqttReasonCode 中的值
    /// </summary>
    public IList<byte> ReasonCodes { get; } = new List<byte>();

    /// <summary>
    /// MQTT 5.0 SUBACK 属性。
    /// </summary>
    public MqttSubAckProperties? Properties { get; set; }

    /// <summary>
    /// 判断所有订阅是否都成功。
    /// </summary>
    public bool AllSuccess
    {
        get
        {
            foreach (var code in ReasonCodes)
            {
                if (code >= 0x80) return false;
            }
            return true;
        }
    }
}
