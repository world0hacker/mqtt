using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.Serialization.Common;

/// <summary>
/// MQTT 二进制数据读取器。
/// 使用 ref struct 避免堆分配，提供高效的 Span 操作。
/// </summary>
/// <remarks>
/// 此类型为 ref struct，只能在栈上分配，不能跨越 await 边界。
/// 所有方法都经过性能优化，使用 AggressiveInlining 内联。
/// </remarks>
public ref struct MqttBinaryReader
{
    private readonly ReadOnlySpan<byte> _buffer;
    private int _position;

    /// <summary>
    /// 创建二进制读取器。
    /// </summary>
    /// <param name="buffer">要读取的数据缓冲区</param>
    public MqttBinaryReader(ReadOnlySpan<byte> buffer)
    {
        _buffer = buffer;
        _position = 0;
    }

    /// <summary>
    /// 获取当前读取位置。
    /// </summary>
    public readonly int Position => _position;

    /// <summary>
    /// 获取剩余可读字节数。
    /// </summary>
    public readonly int Remaining => _buffer.Length - _position;

    /// <summary>
    /// 获取缓冲区总长度。
    /// </summary>
    public readonly int Length => _buffer.Length;

    /// <summary>
    /// 检查是否还有指定数量的字节可读。
    /// </summary>
    /// <param name="count">需要的字节数</param>
    /// <returns>如果剩余字节数足够则返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasBytes(int count) => Remaining >= count;

    /// <summary>
    /// 读取一个字节。
    /// </summary>
    /// <returns>读取的字节值</returns>
    /// <exception cref="IndexOutOfRangeException">当没有足够数据时抛出</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        return _buffer[_position++];
    }

    /// <summary>
    /// 尝试读取一个字节。
    /// </summary>
    /// <param name="value">读取的字节值</param>
    /// <returns>如果成功读取则返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadByte(out byte value)
    {
        if (_position < _buffer.Length)
        {
            value = _buffer[_position++];
            return true;
        }
        value = 0;
        return false;
    }

    /// <summary>
    /// 读取两字节无符号整数（大端序）。
    /// </summary>
    /// <returns>读取的 16 位无符号整数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        var value = (ushort)((_buffer[_position] << 8) | _buffer[_position + 1]);
        _position += 2;
        return value;
    }

    /// <summary>
    /// 尝试读取两字节无符号整数（大端序）。
    /// </summary>
    /// <param name="value">读取的值</param>
    /// <returns>如果成功读取则返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt16(out ushort value)
    {
        if (Remaining >= 2)
        {
            value = (ushort)((_buffer[_position] << 8) | _buffer[_position + 1]);
            _position += 2;
            return true;
        }
        value = 0;
        return false;
    }

    /// <summary>
    /// 读取四字节无符号整数（大端序）。
    /// </summary>
    /// <returns>读取的 32 位无符号整数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        var value = (uint)((_buffer[_position] << 24) |
                          (_buffer[_position + 1] << 16) |
                          (_buffer[_position + 2] << 8) |
                          _buffer[_position + 3]);
        _position += 4;
        return value;
    }

    /// <summary>
    /// 尝试读取四字节无符号整数（大端序）。
    /// </summary>
    /// <param name="value">读取的值</param>
    /// <returns>如果成功读取则返回 true</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt32(out uint value)
    {
        if (Remaining >= 4)
        {
            value = (uint)((_buffer[_position] << 24) |
                          (_buffer[_position + 1] << 16) |
                          (_buffer[_position + 2] << 8) |
                          _buffer[_position + 3]);
            _position += 4;
            return true;
        }
        value = 0;
        return false;
    }

    /// <summary>
    /// 读取可变长度整数（Variable Byte Integer）。
    /// MQTT 协议使用此编码方式表示剩余长度等字段。
    /// </summary>
    /// <returns>解码后的整数值</returns>
    /// <exception cref="MqttProtocolException">当编码无效时抛出</exception>
    public uint ReadVariableByteInteger()
    {
        uint value = 0;
        int multiplier = 1;
        byte encodedByte;
        int bytesRead = 0;

        do
        {
            if (bytesRead >= 4)
            {
                throw new MqttProtocolException("可变长度整数编码无效：超过 4 字节");
            }

            encodedByte = _buffer[_position++];
            value += (uint)((encodedByte & 0x7F) * multiplier);
            multiplier *= 128;
            bytesRead++;
        } while ((encodedByte & 0x80) != 0);

        return value;
    }

    /// <summary>
    /// 尝试读取可变长度整数。
    /// </summary>
    /// <param name="value">读取的值</param>
    /// <returns>如果成功读取则返回 true</returns>
    public bool TryReadVariableByteInteger(out uint value)
    {
        value = 0;
        int multiplier = 1;
        int startPosition = _position;
        int bytesRead = 0;

        while (_position < _buffer.Length)
        {
            if (bytesRead >= 4)
            {
                _position = startPosition;
                return false;
            }

            byte encodedByte = _buffer[_position++];
            value += (uint)((encodedByte & 0x7F) * multiplier);
            multiplier *= 128;
            bytesRead++;

            if ((encodedByte & 0x80) == 0)
            {
                return true;
            }
        }

        // 数据不完整，恢复位置
        _position = startPosition;
        value = 0;
        return false;
    }

    /// <summary>
    /// 读取 UTF-8 编码的字符串。
    /// MQTT 字符串以 2 字节长度前缀开始。
    /// </summary>
    /// <returns>解码后的字符串</returns>
    public string ReadString()
    {
        var length = ReadUInt16();
        if (length == 0)
        {
            return string.Empty;
        }

        var value = Encoding.UTF8.GetString(_buffer.Slice(_position, length));
        _position += length;
        return value;
    }

    /// <summary>
    /// 尝试读取 UTF-8 编码的字符串。
    /// </summary>
    /// <param name="value">读取的字符串</param>
    /// <returns>如果成功读取则返回 true</returns>
    public bool TryReadString(out string value)
    {
        if (Remaining < 2)
        {
            value = string.Empty;
            return false;
        }

        var length = (ushort)((_buffer[_position] << 8) | _buffer[_position + 1]);
        if (Remaining < 2 + length)
        {
            value = string.Empty;
            return false;
        }

        _position += 2;
        value = length == 0 ? string.Empty : Encoding.UTF8.GetString(_buffer.Slice(_position, length));
        _position += length;
        return true;
    }

    /// <summary>
    /// 读取二进制数据。
    /// MQTT 二进制数据以 2 字节长度前缀开始。
    /// </summary>
    /// <returns>读取的二进制数据（复制到新数组）</returns>
    public ReadOnlyMemory<byte> ReadBinaryData()
    {
        var length = ReadUInt16();
        if (length == 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var data = _buffer.Slice(_position, length).ToArray();
        _position += length;
        return data;
    }

    /// <summary>
    /// 读取指定长度的字节切片（零拷贝）。
    /// </summary>
    /// <param name="count">要读取的字节数</param>
    /// <returns>字节切片</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var span = _buffer.Slice(_position, count);
        _position += count;
        return span;
    }

    /// <summary>
    /// 读取剩余所有字节（零拷贝）。
    /// </summary>
    /// <returns>剩余字节切片</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadRemainingBytes()
    {
        var span = _buffer.Slice(_position);
        _position = _buffer.Length;
        return span;
    }

    /// <summary>
    /// 读取剩余所有字节到 Memory（需要复制）。
    /// </summary>
    /// <returns>剩余字节的内存</returns>
    public ReadOnlyMemory<byte> ReadRemainingBytesAsMemory()
    {
        var remaining = Remaining;
        if (remaining == 0)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var data = _buffer.Slice(_position, remaining).ToArray();
        _position = _buffer.Length;
        return data;
    }

    /// <summary>
    /// 跳过指定数量的字节。
    /// </summary>
    /// <param name="count">要跳过的字节数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Skip(int count)
    {
        _position += count;
    }

    /// <summary>
    /// 查看当前位置的字节但不移动位置。
    /// </summary>
    /// <returns>当前位置的字节值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte Peek()
    {
        return _buffer[_position];
    }

    /// <summary>
    /// 查看指定偏移位置的字节但不移动位置。
    /// </summary>
    /// <param name="offset">相对于当前位置的偏移</param>
    /// <returns>指定位置的字节值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte PeekAt(int offset)
    {
        return _buffer[_position + offset];
    }

    /// <summary>
    /// 重置读取位置到开始。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _position = 0;
    }

    /// <summary>
    /// 设置读取位置。
    /// </summary>
    /// <param name="position">新的位置</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seek(int position)
    {
        _position = position;
    }
}
