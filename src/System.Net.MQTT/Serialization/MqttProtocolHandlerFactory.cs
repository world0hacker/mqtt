using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization;

/// <summary>
/// MQTT 协议处理器工厂。
/// 根据协议版本创建对应的协议处理器实例。
/// </summary>
/// <remarks>
/// 处理器实例是线程安全的单例，可在多个连接间共享。
/// </remarks>
public static class MqttProtocolHandlerFactory
{
    // 协议处理器单例实例（延迟初始化）
    private static IMqttProtocolHandler? _v311Handler;
    private static IMqttProtocolHandler? _v500Handler;
    private static readonly object _lock = new();

    /// <summary>
    /// 获取指定协议版本的处理器。
    /// </summary>
    /// <param name="version">协议版本</param>
    /// <returns>对应版本的协议处理器</returns>
    /// <exception cref="NotSupportedException">当协议版本不支持时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IMqttProtocolHandler GetHandler(MqttProtocolVersion version)
    {
        return version switch
        {
            MqttProtocolVersion.V310 => GetV311Handler(), // V3.1.0 使用 V3.1.1 处理器
            MqttProtocolVersion.V311 => GetV311Handler(),
            MqttProtocolVersion.V500 => GetV500Handler(),
            _ => throw new NotSupportedException($"不支持的协议版本: {version}")
        };
    }

    /// <summary>
    /// 根据协议版本字节获取处理器。
    /// </summary>
    /// <param name="versionByte">协议版本字节（3=V3.1.0, 4=V3.1.1, 5=V5.0）</param>
    /// <returns>对应版本的协议处理器</returns>
    /// <exception cref="NotSupportedException">当协议版本不支持时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IMqttProtocolHandler GetHandler(byte versionByte)
    {
        return GetHandler((MqttProtocolVersion)versionByte);
    }

    /// <summary>
    /// 获取 MQTT 3.1.1 协议处理器。
    /// </summary>
    /// <returns>V3.1.1 协议处理器</returns>
    private static IMqttProtocolHandler GetV311Handler()
    {
        if (_v311Handler != null)
            return _v311Handler;

        lock (_lock)
        {
            _v311Handler ??= new V311.V311ProtocolHandler();
            return _v311Handler;
        }
    }

    /// <summary>
    /// 获取 MQTT 5.0 协议处理器。
    /// </summary>
    /// <returns>V5.0 协议处理器</returns>
    private static IMqttProtocolHandler GetV500Handler()
    {
        if (_v500Handler != null)
            return _v500Handler;

        lock (_lock)
        {
            _v500Handler ??= new V500.V500ProtocolHandler();
            return _v500Handler;
        }
    }

    /// <summary>
    /// 判断是否支持指定的协议版本。
    /// </summary>
    /// <param name="version">协议版本</param>
    /// <returns>如果支持返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupported(MqttProtocolVersion version)
    {
        return version is MqttProtocolVersion.V310
            or MqttProtocolVersion.V311
            or MqttProtocolVersion.V500;
    }

    /// <summary>
    /// 判断是否支持指定的协议版本字节。
    /// </summary>
    /// <param name="versionByte">协议版本字节</param>
    /// <returns>如果支持返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSupported(byte versionByte)
    {
        return versionByte is 3 or 4 or 5;
    }
}
