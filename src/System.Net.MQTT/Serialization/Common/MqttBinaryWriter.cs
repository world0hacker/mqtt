using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.Serialization.Common;

/// <summary>
/// MQTT 二进制数据写入器。
/// 使用 ref struct 避免堆分配，提供高效的 Span 操作。
/// </summary>
/// <remarks>
/// 此类型为 ref struct，只能在栈上分配，不能跨越 await 边界。
/// 所有方法都经过性能优化，使用 AggressiveInlining 内联。
/// </remarks>
public ref struct MqttBinaryWriter
{
    private readonly Span<byte> _buffer;
    private int _position;

    /// <summary>
    /// 创建二进制写入器。
    /// </summary>
    /// <param name="buffer">目标写入缓冲区</param>
    public MqttBinaryWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    /// <summary>
    /// 获取当前写入位置（已写入字节数）。
    /// </summary>
    public readonly int Position => _position;

    /// <summary>
    /// 获取剩余可写空间。
    /// </summary>
    public readonly int Remaining => _buffer.Length - _position;

    /// <summary>
    /// 获取缓冲区总长度。
    /// </summary>
    public readonly int Capacity => _buffer.Length;

    /// <summary>
    /// 检查是否还有指定数量的空间可写。
    /// </summary>
    /// <param name="count">需要的字节数</param>
    /// <returns>如果剩余空间足够则返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasSpace(int count) => Remaining >= count;

    /// <summary>
    /// 写入一个字节。
    /// </summary>
    /// <param name="value">要写入的字节值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        _buffer[_position++] = value;
    }

    /// <summary>
    /// 写入两字节无符号整数（大端序）。
    /// </summary>
    /// <param name="value">要写入的 16 位无符号整数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16(ushort value)
    {
        _buffer[_position++] = (byte)(value >> 8);
        _buffer[_position++] = (byte)(value & 0xFF);
    }

    /// <summary>
    /// 写入四字节无符号整数（大端序）。
    /// </summary>
    /// <param name="value">要写入的 32 位无符号整数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32(uint value)
    {
        _buffer[_position++] = (byte)(value >> 24);
        _buffer[_position++] = (byte)(value >> 16);
        _buffer[_position++] = (byte)(value >> 8);
        _buffer[_position++] = (byte)(value & 0xFF);
    }

    /// <summary>
    /// 写入可变长度整数（Variable Byte Integer）。
    /// MQTT 协议使用此编码方式表示剩余长度等字段。
    /// </summary>
    /// <param name="value">要编码的整数值（0-268435455）</param>
    /// <exception cref="ArgumentOutOfRangeException">当值超出有效范围时抛出</exception>
    public void WriteVariableByteInteger(uint value)
    {
        if (value > 268435455)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "可变长度整数不能超过 268435455");
        }

        do
        {
            byte encodedByte = (byte)(value % 128);
            value /= 128;
            if (value > 0)
            {
                encodedByte |= 0x80;
            }
            _buffer[_position++] = encodedByte;
        } while (value > 0);
    }

    /// <summary>
    /// 计算可变长度整数编码后的字节数。
    /// </summary>
    /// <param name="value">要计算的整数值</param>
    /// <returns>编码所需的字节数（1-4）</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetVariableByteIntegerSize(uint value)
    {
        if (value < 128) return 1;           // 0x00 - 0x7F
        if (value < 16384) return 2;         // 0x80 - 0x3FFF
        if (value < 2097152) return 3;       // 0x4000 - 0x1FFFFF
        return 4;                             // 0x200000 - 0xFFFFFFF
    }

    /// <summary>
    /// 写入 UTF-8 编码的字符串。
    /// MQTT 字符串以 2 字节长度前缀开始。
    /// </summary>
    /// <param name="value">要写入的字符串</param>
    public void WriteString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteUInt16(0);
            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(value);
        WriteUInt16((ushort)byteCount);
        Encoding.UTF8.GetBytes(value, _buffer.Slice(_position, byteCount));
        _position += byteCount;
    }

    /// <summary>
    /// 计算 UTF-8 字符串编码后的字节数（含长度前缀）。
    /// </summary>
    /// <param name="value">要计算的字符串</param>
    /// <returns>编码所需的总字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetStringSize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 2; // 仅长度前缀
        }
        return 2 + Encoding.UTF8.GetByteCount(value);
    }

    /// <summary>
    /// 写入二进制数据。
    /// MQTT 二进制数据以 2 字节长度前缀开始。
    /// </summary>
    /// <param name="data">要写入的二进制数据</param>
    public void WriteBinaryData(ReadOnlySpan<byte> data)
    {
        WriteUInt16((ushort)data.Length);
        if (data.Length > 0)
        {
            data.CopyTo(_buffer.Slice(_position));
            _position += data.Length;
        }
    }

    /// <summary>
    /// 计算二进制数据编码后的字节数（含长度前缀）。
    /// </summary>
    /// <param name="data">要计算的数据</param>
    /// <returns>编码所需的总字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBinaryDataSize(ReadOnlySpan<byte> data)
    {
        return 2 + data.Length;
    }

    /// <summary>
    /// 计算二进制数据编码后的字节数（含长度前缀）。
    /// </summary>
    /// <param name="data">要计算的数据</param>
    /// <returns>编码所需的总字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBinaryDataSize(ReadOnlyMemory<byte> data)
    {
        return 2 + data.Length;
    }

    /// <summary>
    /// 写入字节数组（不含长度前缀）。
    /// </summary>
    /// <param name="data">要写入的字节数据</param>
    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length > 0)
        {
            data.CopyTo(_buffer.Slice(_position));
            _position += data.Length;
        }
    }

    /// <summary>
    /// 在指定位置写入一个字节（不移动当前位置）。
    /// </summary>
    /// <param name="position">写入位置</param>
    /// <param name="value">要写入的字节值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByteAt(int position, byte value)
    {
        _buffer[position] = value;
    }

    /// <summary>
    /// 在指定位置写入两字节无符号整数（不移动当前位置）。
    /// </summary>
    /// <param name="position">写入位置</param>
    /// <param name="value">要写入的值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16At(int position, ushort value)
    {
        _buffer[position] = (byte)(value >> 8);
        _buffer[position + 1] = (byte)(value & 0xFF);
    }

    /// <summary>
    /// 获取已写入数据的切片。
    /// </summary>
    /// <returns>已写入数据的 Span</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> GetWrittenSpan()
    {
        return _buffer.Slice(0, _position);
    }

    /// <summary>
    /// 重置写入位置到开始。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _position = 0;
    }

    /// <summary>
    /// 设置写入位置。
    /// </summary>
    /// <param name="position">新的位置</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seek(int position)
    {
        _position = position;
    }

    /// <summary>
    /// 前进指定字节数（预留空间）。
    /// </summary>
    /// <param name="count">要前进的字节数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        _position += count;
    }
}
