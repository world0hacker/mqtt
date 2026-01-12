using System.Net.MQTT.Protocol.Properties;

namespace System.Net.MQTT.Protocol.Packets;

/// <summary>
/// MQTT PUBLISH 报文。
/// 用于传输应用消息。
/// </summary>
public sealed class MqttPublishPacket : IMqttPacketWithId
{
    /// <inheritdoc/>
    public MqttPacketType PacketType => MqttPacketType.Publish;

    /// <inheritdoc/>
    public ushort PacketId { get; set; }

    /// <summary>
    /// 主题名称。
    /// 消息发布的目标主题。
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// QoS 级别。
    /// </summary>
    public MqttQualityOfService QoS { get; set; }

    /// <summary>
    /// 重复发送标志。
    /// true 表示这是一个重复发送的消息（用于 QoS > 0）。
    /// </summary>
    public bool Duplicate { get; set; }

    /// <summary>
    /// 保留消息标志。
    /// true 表示服务器应该保留此消息。
    /// </summary>
    public bool Retain { get; set; }

    /// <summary>
    /// 消息载荷。
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; set; }

    /// <summary>
    /// MQTT 5.0 PUBLISH 属性。
    /// </summary>
    public MqttPublishProperties? Properties { get; set; }

    /// <summary>
    /// 获取固定头部标志位。
    /// </summary>
    /// <returns>标志位（低 4 位）</returns>
    public byte GetFlags()
    {
        byte flags = 0;
        if (Duplicate) flags |= 0x08;
        flags |= (byte)((int)QoS << 1);
        if (Retain) flags |= 0x01;
        return flags;
    }

    /// <summary>
    /// 从标志位设置属性。
    /// </summary>
    /// <param name="flags">固定头部标志位</param>
    public void SetFromFlags(byte flags)
    {
        Duplicate = (flags & 0x08) != 0;
        QoS = (MqttQualityOfService)((flags >> 1) & 0x03);
        Retain = (flags & 0x01) != 0;
    }
}
