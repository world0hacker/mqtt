using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT UNSUBSCRIBE 报文。
/// 客户端取消订阅主题时发送。
/// </summary>
public sealed class MqttUnsubscribePacket : IMqttPacketWithId
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.Unsubscribe;

    /// <inheritdoc/>
    public ushort PacketId { get; set; }

    /// <summary>
    /// 要取消订阅的主题过滤器列表。
    /// </summary>
    public IList<string> TopicFilters { get; } = new List<string>();

    /// <summary>
    /// MQTT 5.0 UNSUBSCRIBE 属性。
    /// </summary>
    public MqttUnsubscribeProperties? Properties { get; set; }
}
