using System.Buffers;
using System.Net.MQTT.Protocol;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// MQTT 报文解析器基础接口。
/// 定义从二进制数据解析报文的契约。
/// </summary>
/// <typeparam name="TPacket">报文类型</typeparam>
public interface IMqttPacketParser<TPacket> where TPacket : IMqttPacket
{
    /// <summary>
    /// 从只读序列中解析报文。
    /// 用于处理可能跨越多个内存段的大报文。
    /// </summary>
    /// <param name="data">报文数据（不含固定头部）</param>
    /// <param name="flags">固定头部标志位（低 4 位）</param>
    /// <returns>解析后的报文对象</returns>
    /// <exception cref="MqttProtocolException">当报文格式无效时抛出</exception>
    TPacket Parse(ReadOnlySequence<byte> data, byte flags);

    /// <summary>
    /// 从只读 Span 中同步解析报文。
    /// 用于小报文的优化解析路径。
    /// </summary>
    /// <param name="data">报文数据（不含固定头部）</param>
    /// <param name="flags">固定头部标志位（低 4 位）</param>
    /// <returns>解析后的报文对象</returns>
    /// <exception cref="MqttProtocolException">当报文格式无效时抛出</exception>
    TPacket Parse(ReadOnlySpan<byte> data, byte flags);
}
