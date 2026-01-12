using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.Common;

/// <summary>
/// PINGREQ/PINGRESP 报文处理器实现。
/// 所有协议版本共用此实现。
/// </summary>
public sealed class MqttPingPacketHandler : IPingPacketHandler
{
    /// <summary>
    /// 单例实例。
    /// </summary>
    public static readonly MqttPingPacketHandler Instance = new();

    // 预分配的报文字节数组
    private static readonly byte[] PingReqBytes = { 0xC0, 0x00 };  // PINGREQ: 类型=12, 剩余长度=0
    private static readonly byte[] PingRespBytes = { 0xD0, 0x00 }; // PINGRESP: 类型=13, 剩余长度=0

    /// <summary>
    /// 私有构造函数，使用 Instance 获取实例。
    /// </summary>
    private MqttPingPacketHandler() { }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WritePingReq(Span<byte> buffer)
    {
        buffer[0] = 0xC0;
        buffer[1] = 0x00;
        return 2;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int WritePingResp(Span<byte> buffer)
    {
        buffer[0] = 0xD0;
        buffer[1] = 0x00;
        return 2;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] GetPingReqBytes() => PingReqBytes;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] GetPingRespBytes() => PingRespBytes;
}
