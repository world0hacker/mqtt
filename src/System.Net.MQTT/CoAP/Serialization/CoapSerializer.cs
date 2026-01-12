using System.Buffers;
using System.Net.MQTT.CoAP.Protocol;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.CoAP.Serialization;

/// <summary>
/// CoAP 消息序列化器。
/// 高性能的序列化和反序列化实现。
/// </summary>
public static class CoapSerializer
{
    /// <summary>
    /// 从数据报反序列化 CoAP 消息。
    /// </summary>
    /// <param name="datagram">数据报缓冲区</param>
    /// <returns>解析的 CoAP 消息</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static CoapMessage Deserialize(ReadOnlySpan<byte> datagram)
    {
        if (datagram.Length < 4)
        {
            throw new ArgumentException("CoAP 消息长度不足", nameof(datagram));
        }

        var message = new CoapMessage();
        int offset = 0;

        // 第一个字节: Ver(2) | Type(2) | TKL(4)
        var firstByte = datagram[offset++];
        var version = (firstByte >> 6) & 0x03;
        if (version != 1)
        {
            throw new ArgumentException($"不支持的 CoAP 版本: {version}", nameof(datagram));
        }

        message.Type = (CoapMessageType)((firstByte >> 4) & 0x03);
        var tokenLength = firstByte & 0x0F;
        if (tokenLength > 8)
        {
            throw new ArgumentException($"无效的令牌长度: {tokenLength}", nameof(datagram));
        }

        // 第二个字节: Code
        message.Code = datagram[offset++];

        // 第三、四个字节: Message ID
        message.MessageId = (ushort)((datagram[offset] << 8) | datagram[offset + 1]);
        offset += 2;

        // 令牌
        if (tokenLength > 0)
        {
            if (offset + tokenLength > datagram.Length)
            {
                throw new ArgumentException("消息长度不足以包含令牌", nameof(datagram));
            }
            message.Token = datagram.Slice(offset, tokenLength).ToArray();
            offset += tokenLength;
        }

        // 选项
        int lastOptionNumber = 0;
        while (offset < datagram.Length)
        {
            var optionByte = datagram[offset];

            // 有效载荷标记
            if (optionByte == CoapMessage.PayloadMarker)
            {
                offset++;
                break;
            }

            offset++;

            // 解析选项 delta 和 length
            var optionDelta = (optionByte >> 4) & 0x0F;
            var optionLength = optionByte & 0x0F;

            // 扩展 delta
            if (optionDelta == 13)
            {
                optionDelta = datagram[offset++] + 13;
            }
            else if (optionDelta == 14)
            {
                optionDelta = ((datagram[offset] << 8) | datagram[offset + 1]) + 269;
                offset += 2;
            }
            else if (optionDelta == 15)
            {
                throw new ArgumentException("无效的选项 delta 值 15", nameof(datagram));
            }

            // 扩展 length
            if (optionLength == 13)
            {
                optionLength = datagram[offset++] + 13;
            }
            else if (optionLength == 14)
            {
                optionLength = ((datagram[offset] << 8) | datagram[offset + 1]) + 269;
                offset += 2;
            }
            else if (optionLength == 15)
            {
                throw new ArgumentException("无效的选项长度值 15", nameof(datagram));
            }

            // 计算选项编号
            var optionNumber = lastOptionNumber + optionDelta;
            lastOptionNumber = optionNumber;

            // 读取选项值
            var optionValue = optionLength > 0
                ? datagram.Slice(offset, optionLength).ToArray()
                : Array.Empty<byte>();
            offset += optionLength;

            message.Options.Add(new CoapOption(optionNumber, optionValue));
        }

        // 有效载荷
        if (offset < datagram.Length)
        {
            message.Payload = datagram.Slice(offset).ToArray();
        }

        return message;
    }

    /// <summary>
    /// 序列化 CoAP 消息到缓冲区。
    /// </summary>
    /// <param name="message">要序列化的消息</param>
    /// <param name="buffer">目标缓冲区</param>
    /// <returns>写入的字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int Serialize(CoapMessage message, Span<byte> buffer)
    {
        int offset = 0;

        // 第一个字节: Ver(2) | Type(2) | TKL(4)
        buffer[offset++] = (byte)((CoapMessage.Version << 6) | ((int)message.Type << 4) | message.TokenLength);

        // 第二个字节: Code
        buffer[offset++] = message.Code;

        // 第三、四个字节: Message ID
        buffer[offset++] = (byte)(message.MessageId >> 8);
        buffer[offset++] = (byte)message.MessageId;

        // 令牌
        if (message.TokenLength > 0)
        {
            message.Token.CopyTo(buffer.Slice(offset));
            offset += message.TokenLength;
        }

        // 选项（按编号排序）
        var sortedOptions = message.Options.OrderBy(o => o.Number).ToList();
        int lastOptionNumber = 0;

        foreach (var option in sortedOptions)
        {
            var optionDelta = option.Number - lastOptionNumber;
            var optionLength = option.Length;
            lastOptionNumber = option.Number;

            // 计算 delta 编码
            int deltaExt = 0;
            int deltaExtLength = 0;
            int deltaValue;
            if (optionDelta < 13)
            {
                deltaValue = optionDelta;
            }
            else if (optionDelta < 269)
            {
                deltaValue = 13;
                deltaExt = optionDelta - 13;
                deltaExtLength = 1;
            }
            else
            {
                deltaValue = 14;
                deltaExt = optionDelta - 269;
                deltaExtLength = 2;
            }

            // 计算 length 编码
            int lengthExt = 0;
            int lengthExtLength = 0;
            int lengthValue;
            if (optionLength < 13)
            {
                lengthValue = optionLength;
            }
            else if (optionLength < 269)
            {
                lengthValue = 13;
                lengthExt = optionLength - 13;
                lengthExtLength = 1;
            }
            else
            {
                lengthValue = 14;
                lengthExt = optionLength - 269;
                lengthExtLength = 2;
            }

            // 写入选项头
            buffer[offset++] = (byte)((deltaValue << 4) | lengthValue);

            // 写入扩展 delta
            if (deltaExtLength == 1)
            {
                buffer[offset++] = (byte)deltaExt;
            }
            else if (deltaExtLength == 2)
            {
                buffer[offset++] = (byte)(deltaExt >> 8);
                buffer[offset++] = (byte)deltaExt;
            }

            // 写入扩展 length
            if (lengthExtLength == 1)
            {
                buffer[offset++] = (byte)lengthExt;
            }
            else if (lengthExtLength == 2)
            {
                buffer[offset++] = (byte)(lengthExt >> 8);
                buffer[offset++] = (byte)lengthExt;
            }

            // 写入选项值
            if (optionLength > 0)
            {
                option.Value.Span.CopyTo(buffer.Slice(offset));
                offset += optionLength;
            }
        }

        // 有效载荷
        if (message.Payload.Length > 0)
        {
            buffer[offset++] = CoapMessage.PayloadMarker;
            message.Payload.CopyTo(buffer.Slice(offset));
            offset += message.Payload.Length;
        }

        return offset;
    }

    /// <summary>
    /// 计算序列化消息所需的缓冲区大小。
    /// </summary>
    /// <param name="message">消息</param>
    /// <returns>所需字节数</returns>
    public static int CalculateSize(CoapMessage message)
    {
        int size = 4 + message.TokenLength; // 头部 + 令牌

        // 选项
        int lastOptionNumber = 0;
        foreach (var option in message.Options.OrderBy(o => o.Number))
        {
            var optionDelta = option.Number - lastOptionNumber;
            var optionLength = option.Length;
            lastOptionNumber = option.Number;

            size += 1; // 选项头

            // Delta 扩展
            if (optionDelta >= 269) size += 2;
            else if (optionDelta >= 13) size += 1;

            // Length 扩展
            if (optionLength >= 269) size += 2;
            else if (optionLength >= 13) size += 1;

            size += optionLength;
        }

        // 有效载荷
        if (message.Payload.Length > 0)
        {
            size += 1 + message.Payload.Length; // 标记 + 载荷
        }

        return size;
    }

    /// <summary>
    /// 序列化 CoAP 消息，使用 ArrayPool 分配内存。
    /// 调用者负责归还缓冲区。
    /// </summary>
    /// <param name="message">要序列化的消息</param>
    /// <param name="rentedBuffer">租用的缓冲区</param>
    /// <returns>实际使用的字节数</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SerializeWithPooledBuffer(CoapMessage message, out byte[] rentedBuffer)
    {
        var size = CalculateSize(message);
        rentedBuffer = ArrayPool<byte>.Shared.Rent(size);
        return Serialize(message, rentedBuffer);
    }
}
