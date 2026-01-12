using System.Buffers;
using System.Net.MQTT.Protocol;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// MQTT 报文构建器基础接口。
/// 定义将报文对象编码为二进制数据的契约。
/// </summary>
/// <typeparam name="TPacket">报文类型</typeparam>
public interface IMqttPacketBuilder<TPacket> where TPacket : IMqttPacket
{
    /// <summary>
    /// 计算报文编码后的字节数（不含固定头部）。
    /// 用于预分配精确大小的缓冲区。
    /// </summary>
    /// <param name="packet">要编码的报文</param>
    /// <returns>变长头部和载荷的总字节数</returns>
    int CalculateSize(TPacket packet);

    /// <summary>
    /// 将报文编码到缓冲区（不含固定头部）。
    /// </summary>
    /// <param name="packet">要编码的报文</param>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>实际写入的字节数</returns>
    int Build(TPacket packet, Span<byte> buffer);

    /// <summary>
    /// 获取报文的固定头部标志位。
    /// </summary>
    /// <param name="packet">报文</param>
    /// <returns>标志位（低 4 位）</returns>
    byte GetFlags(TPacket packet);

    /// <summary>
    /// 异步将完整报文写入 IBufferWriter。
    /// 包含固定头部、剩余长度和报文内容。
    /// </summary>
    /// <param name="packet">要编码的报文</param>
    /// <param name="writer">目标写入器</param>
    void WriteTo(TPacket packet, IBufferWriter<byte> writer);
}
