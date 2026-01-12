using System.Runtime.CompilerServices;
using System.Net.MQTT.Serialization;

namespace System.Net.MQTT.MqttSn.Protocol;

/// <summary>
/// MQTT-SN 标志位结构。
/// 用于 PUBLISH、SUBSCRIBE 等报文。
///
/// 标志位布局 (1 字节):
/// |  7  |  6  |  5  |  4  |  3  |  2  |  1  |  0  |
/// | DUP | QoS     | Retain | Will | CleanSession | TopicType |
/// </summary>
public readonly struct MqttSnFlags
{
    private readonly byte _value;

    /// <summary>
    /// 创建 MQTT-SN 标志位。
    /// </summary>
    /// <param name="value">原始字节值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MqttSnFlags(byte value)
    {
        _value = value;
    }

    /// <summary>
    /// 获取原始字节值。
    /// </summary>
    public byte Value => _value;

    /// <summary>
    /// 获取重复标志。表示消息是否为重传。
    /// </summary>
    public bool Dup => (_value & 0x80) != 0;

    /// <summary>
    /// 获取服务质量等级。
    /// </summary>
    public MqttQualityOfService QoS => (MqttQualityOfService)((_value >> 5) & 0x03);

    /// <summary>
    /// 获取保留标志。表示消息是否应被保留。
    /// </summary>
    public bool Retain => (_value & 0x10) != 0;

    /// <summary>
    /// 获取遗嘱标志。仅用于 CONNECT 报文。
    /// </summary>
    public bool Will => (_value & 0x08) != 0;

    /// <summary>
    /// 获取清理会话标志。仅用于 CONNECT 报文。
    /// </summary>
    public bool CleanSession => (_value & 0x04) != 0;

    /// <summary>
    /// 获取主题类型。
    /// </summary>
    public MqttSnTopicType TopicType => (MqttSnTopicType)(_value & 0x03);

    /// <summary>
    /// 创建标志位构建器。
    /// </summary>
    /// <returns>标志位构建器</returns>
    public static MqttSnFlagsBuilder Create() => new();

    /// <summary>
    /// 从字节值创建标志位。
    /// </summary>
    /// <param name="value">字节值</param>
    /// <returns>标志位实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MqttSnFlags FromByte(byte value) => new(value);

    /// <summary>
    /// 隐式转换为字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(MqttSnFlags flags) => flags._value;

    /// <summary>
    /// 隐式转换从字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator MqttSnFlags(byte value) => new(value);

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"[DUP={Dup}, QoS={QoS}, Retain={Retain}, Will={Will}, CleanSession={CleanSession}, TopicType={TopicType}]";
    }
}

/// <summary>
/// MQTT-SN 标志位构建器。
/// </summary>
public struct MqttSnFlagsBuilder
{
    private byte _value;

    /// <summary>
    /// 设置重复标志。
    /// </summary>
    /// <param name="dup">是否重复</param>
    /// <returns>构建器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MqttSnFlagsBuilder WithDup(bool dup)
    {
        if (dup)
            _value |= 0x80;
        else
            _value &= 0x7F;
        return this;
    }

    /// <summary>
    /// 设置服务质量等级。
    /// </summary>
    /// <param name="qos">QoS 等级</param>
    /// <returns>构建器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MqttSnFlagsBuilder WithQoS(MqttQualityOfService qos)
    {
        _value = (byte)((_value & 0x9F) | (((byte)qos & 0x03) << 5));
        return this;
    }

    /// <summary>
    /// 设置保留标志。
    /// </summary>
    /// <param name="retain">是否保留</param>
    /// <returns>构建器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MqttSnFlagsBuilder WithRetain(bool retain)
    {
        if (retain)
            _value |= 0x10;
        else
            _value &= 0xEF;
        return this;
    }

    /// <summary>
    /// 设置遗嘱标志。
    /// </summary>
    /// <param name="will">是否有遗嘱</param>
    /// <returns>构建器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MqttSnFlagsBuilder WithWill(bool will)
    {
        if (will)
            _value |= 0x08;
        else
            _value &= 0xF7;
        return this;
    }

    /// <summary>
    /// 设置清理会话标志。
    /// </summary>
    /// <param name="cleanSession">是否清理会话</param>
    /// <returns>构建器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MqttSnFlagsBuilder WithCleanSession(bool cleanSession)
    {
        if (cleanSession)
            _value |= 0x04;
        else
            _value &= 0xFB;
        return this;
    }

    /// <summary>
    /// 设置主题类型。
    /// </summary>
    /// <param name="topicType">主题类型</param>
    /// <returns>构建器</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MqttSnFlagsBuilder WithTopicType(MqttSnTopicType topicType)
    {
        _value = (byte)((_value & 0xFC) | ((byte)topicType & 0x03));
        return this;
    }

    /// <summary>
    /// 构建标志位。
    /// </summary>
    /// <returns>标志位实例</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MqttSnFlags Build() => new(_value);
}
