using System.Net.MQTT.Protocol.Packets;

namespace System.Net.MQTT.Serialization.Interfaces;

/// <summary>
/// PINGREQ/PINGRESP 报文处理器接口。
/// 这两个报文没有可变头部和载荷，因此合并为一个处理器。
/// </summary>
public interface IPingPacketHandler
{
    /// <summary>
    /// 将 PINGREQ 报文写入缓冲区。
    /// </summary>
    /// <param name="buffer">目标缓冲区（至少 2 字节）</param>
    /// <returns>写入的字节数（固定为 2）</returns>
    int WritePingReq(Span<byte> buffer);

    /// <summary>
    /// 将 PINGRESP 报文写入缓冲区。
    /// </summary>
    /// <param name="buffer">目标缓冲区（至少 2 字节）</param>
    /// <returns>写入的字节数（固定为 2）</returns>
    int WritePingResp(Span<byte> buffer);

    /// <summary>
    /// 获取 PINGREQ 报文字节。
    /// </summary>
    /// <returns>PINGREQ 报文字节数组</returns>
    byte[] GetPingReqBytes();

    /// <summary>
    /// 获取 PINGRESP 报文字节。
    /// </summary>
    /// <returns>PINGRESP 报文字节数组</returns>
    byte[] GetPingRespBytes();
}
