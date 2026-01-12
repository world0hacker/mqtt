using System.Buffers;
using System.Net.MQTT.Protocol;
using System.Net.MQTT.Serialization.Interfaces;

namespace System.Net.MQTT.Serialization;

/// <summary>
/// MQTT 协议处理器接口。
/// 提供特定协议版本的所有解析器和构建器。
/// 这是实现协议版本抽象的核心接口。
/// </summary>
public interface IMqttProtocolHandler
{
    /// <summary>
    /// 获取协议版本。
    /// </summary>
    MqttProtocolVersion ProtocolVersion { get; }

    /// <summary>
    /// 获取是否支持 AUTH 报文（仅 MQTT 5.0）。
    /// </summary>
    bool SupportsAuth { get; }

    /// <summary>
    /// 获取是否支持属性（仅 MQTT 5.0）。
    /// </summary>
    bool SupportsProperties { get; }

    #region 解析器

    /// <summary>CONNECT 报文解析器</summary>
    IConnectPacketParser ConnectParser { get; }

    /// <summary>CONNACK 报文解析器</summary>
    IConnAckPacketParser ConnAckParser { get; }

    /// <summary>PUBLISH 报文解析器</summary>
    IPublishPacketParser PublishParser { get; }

    /// <summary>PUBACK/PUBREC/PUBREL/PUBCOMP 报文解析器</summary>
    IPubAckPacketParser PubAckParser { get; }

    /// <summary>SUBSCRIBE 报文解析器</summary>
    ISubscribePacketParser SubscribeParser { get; }

    /// <summary>SUBACK 报文解析器</summary>
    ISubAckPacketParser SubAckParser { get; }

    /// <summary>UNSUBSCRIBE 报文解析器</summary>
    IUnsubscribePacketParser UnsubscribeParser { get; }

    /// <summary>UNSUBACK 报文解析器</summary>
    IUnsubAckPacketParser UnsubAckParser { get; }

    /// <summary>DISCONNECT 报文解析器</summary>
    IDisconnectPacketParser DisconnectParser { get; }

    /// <summary>AUTH 报文解析器（仅 MQTT 5.0，其他版本为 null）</summary>
    IAuthPacketParser? AuthParser { get; }

    #endregion

    #region 构建器

    /// <summary>CONNECT 报文构建器</summary>
    IConnectPacketBuilder ConnectBuilder { get; }

    /// <summary>CONNACK 报文构建器</summary>
    IConnAckPacketBuilder ConnAckBuilder { get; }

    /// <summary>PUBLISH 报文构建器</summary>
    IPublishPacketBuilder PublishBuilder { get; }

    /// <summary>PUBACK/PUBREC/PUBREL/PUBCOMP 报文构建器</summary>
    IPubAckPacketBuilder PubAckBuilder { get; }

    /// <summary>SUBSCRIBE 报文构建器</summary>
    ISubscribePacketBuilder SubscribeBuilder { get; }

    /// <summary>SUBACK 报文构建器</summary>
    ISubAckPacketBuilder SubAckBuilder { get; }

    /// <summary>UNSUBSCRIBE 报文构建器</summary>
    IUnsubscribePacketBuilder UnsubscribeBuilder { get; }

    /// <summary>UNSUBACK 报文构建器</summary>
    IUnsubAckPacketBuilder UnsubAckBuilder { get; }

    /// <summary>DISCONNECT 报文构建器</summary>
    IDisconnectPacketBuilder DisconnectBuilder { get; }

    /// <summary>AUTH 报文构建器（仅 MQTT 5.0，其他版本为 null）</summary>
    IAuthPacketBuilder? AuthBuilder { get; }

    /// <summary>PING 报文处理器</summary>
    IPingPacketHandler PingHandler { get; }

    #endregion

    #region 高层方法

    /// <summary>
    /// 解析报文。
    /// </summary>
    /// <param name="packetType">报文类型</param>
    /// <param name="flags">固定头部标志位</param>
    /// <param name="data">报文数据（不含固定头部）</param>
    /// <returns>解析后的报文</returns>
    IMqttPacket ParsePacket(MqttPacketType packetType, byte flags, ReadOnlySequence<byte> data);

    /// <summary>
    /// 解析报文（Span 版本）。
    /// </summary>
    /// <param name="packetType">报文类型</param>
    /// <param name="flags">固定头部标志位</param>
    /// <param name="data">报文数据（不含固定头部）</param>
    /// <returns>解析后的报文</returns>
    IMqttPacket ParsePacket(MqttPacketType packetType, byte flags, ReadOnlySpan<byte> data);

    /// <summary>
    /// 将报文写入 IBufferWriter。
    /// </summary>
    /// <param name="packet">要写入的报文</param>
    /// <param name="writer">目标写入器</param>
    void WritePacket(IMqttPacket packet, IBufferWriter<byte> writer);

    /// <summary>
    /// 异步从流中读取报文。
    /// </summary>
    /// <param name="stream">数据流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取的报文，如果连接关闭返回 null</returns>
    ValueTask<IMqttPacket?> ReadPacketAsync(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步将报文写入流。
    /// </summary>
    /// <param name="packet">要写入的报文</param>
    /// <param name="stream">目标流</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask WritePacketAsync(IMqttPacket packet, Stream stream, CancellationToken cancellationToken = default);

    #endregion
}
