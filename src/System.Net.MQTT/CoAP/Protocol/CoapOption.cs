using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.CoAP.Protocol;

/// <summary>
/// CoAP 选项。
/// </summary>
public readonly struct CoapOption
{
    /// <summary>
    /// 获取选项编号。
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// 获取选项值。
    /// </summary>
    public ReadOnlyMemory<byte> Value { get; }

    /// <summary>
    /// 创建 CoAP 选项。
    /// </summary>
    /// <param name="number">选项编号</param>
    /// <param name="value">选项值</param>
    public CoapOption(int number, ReadOnlyMemory<byte> value)
    {
        Number = number;
        Value = value;
    }

    /// <summary>
    /// 创建空值选项。
    /// </summary>
    /// <param name="number">选项编号</param>
    public CoapOption(int number) : this(number, ReadOnlyMemory<byte>.Empty)
    {
    }

    /// <summary>
    /// 创建字符串值选项。
    /// </summary>
    /// <param name="number">选项编号</param>
    /// <param name="value">字符串值</param>
    public CoapOption(int number, string value) : this(number, Encoding.UTF8.GetBytes(value))
    {
    }

    /// <summary>
    /// 创建整数值选项。
    /// </summary>
    /// <param name="number">选项编号</param>
    /// <param name="value">整数值</param>
    public CoapOption(int number, uint value) : this(number, EncodeUint(value))
    {
    }

    /// <summary>
    /// 获取选项值是否为空。
    /// </summary>
    public bool IsEmpty => Value.IsEmpty;

    /// <summary>
    /// 获取选项值长度。
    /// </summary>
    public int Length => Value.Length;

    /// <summary>
    /// 获取选项是否为关键选项。
    /// </summary>
    public bool IsCritical => CoapOptionNumber.IsCritical(Number);

    /// <summary>
    /// 获取选项值为字符串。
    /// </summary>
    /// <returns>字符串值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetStringValue()
    {
        if (Value.IsEmpty) return string.Empty;
        return Encoding.UTF8.GetString(Value.Span);
    }

    /// <summary>
    /// 获取选项值为无符号整数。
    /// </summary>
    /// <returns>无符号整数值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint GetUintValue()
    {
        if (Value.IsEmpty) return 0;

        uint result = 0;
        var span = Value.Span;
        for (int i = 0; i < span.Length; i++)
        {
            result = (result << 8) | span[i];
        }
        return result;
    }

    /// <summary>
    /// 编码无符号整数为字节数组。
    /// CoAP 使用变长整数编码。
    /// </summary>
    private static byte[] EncodeUint(uint value)
    {
        if (value == 0) return Array.Empty<byte>();
        if (value <= 0xFF) return new[] { (byte)value };
        if (value <= 0xFFFF) return new[] { (byte)(value >> 8), (byte)value };
        if (value <= 0xFFFFFF) return new[] { (byte)(value >> 16), (byte)(value >> 8), (byte)value };
        return new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Option[{Number}]={GetStringValue()}";
    }
}
