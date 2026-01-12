namespace System.Net.MQTT.MqttSn.Protocol;

/// <summary>
/// MQTT-SN 报文接口。
/// </summary>
public interface IMqttSnPacket
{
    /// <summary>
    /// 获取报文类型。
    /// </summary>
    MqttSnPacketType PacketType { get; }

    /// <summary>
    /// 获取报文长度（包括长度字段本身）。
    /// </summary>
    int Length { get; }

    /// <summary>
    /// 将报文序列化到缓冲区。
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>写入的字节数</returns>
    int WriteTo(Span<byte> buffer);
}
