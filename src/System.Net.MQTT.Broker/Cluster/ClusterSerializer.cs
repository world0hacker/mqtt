using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net.MQTT.Broker.Cluster;

/// <summary>
/// 集群消息序列化器。
/// 使用轻量级二进制协议进行节点间通信。
/// </summary>
/// <remarks>
/// 消息格式：
/// [1 字节] 消息类型
/// [4 字节] 消息长度（大端序）
/// [N 字节] 消息内容
///
/// 消息内容格式（Publish 类型）：
/// [2 字节] SourceNodeId 长度
/// [N 字节] SourceNodeId
/// [2 字节] MessageId 长度
/// [N 字节] MessageId
/// [2 字节] Topic 长度
/// [N 字节] Topic
/// [1 字节] QoS + Retain 标志
/// [4 字节] Payload 长度
/// [N 字节] Payload
/// [8 字节] Timestamp
/// </remarks>
public static class ClusterSerializer
{
    private const int HeaderSize = 5; // 1 字节类型 + 4 字节长度

    /// <summary>
    /// 序列化集群消息。
    /// </summary>
    /// <param name="message">集群消息</param>
    /// <returns>序列化后的字节数组</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static byte[] Serialize(ClusterMessage message)
    {
        // 计算消息内容大小
        var contentSize = CalculateContentSize(message);
        var buffer = new byte[HeaderSize + contentSize];
        var span = buffer.AsSpan();

        // 写入头部
        span[0] = (byte)message.Type;
        WriteInt32BigEndian(span[1..], contentSize);

        // 写入内容
        var offset = HeaderSize;
        WriteContent(span, ref offset, message);

        return buffer;
    }

    /// <summary>
    /// 反序列化集群消息头部。
    /// </summary>
    /// <param name="header">头部数据（5 字节）</param>
    /// <param name="messageType">消息类型</param>
    /// <param name="contentLength">内容长度</param>
    /// <returns>是否成功</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParseHeader(ReadOnlySpan<byte> header, out ClusterMessageType messageType, out int contentLength)
    {
        if (header.Length < HeaderSize)
        {
            messageType = default;
            contentLength = 0;
            return false;
        }

        messageType = (ClusterMessageType)header[0];
        contentLength = ReadInt32BigEndian(header[1..]);
        return true;
    }

    /// <summary>
    /// 反序列化集群消息内容。
    /// </summary>
    /// <param name="type">消息类型</param>
    /// <param name="content">消息内容</param>
    /// <returns>集群消息</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static ClusterMessage Deserialize(ClusterMessageType type, ReadOnlySpan<byte> content)
    {
        var message = new ClusterMessage { Type = type };
        var offset = 0;

        switch (type)
        {
            case ClusterMessageType.Heartbeat:
                message.SourceNodeId = ReadString(content, ref offset);
                message.Timestamp = ReadInt64BigEndian(content[offset..]);
                break;

            case ClusterMessageType.Publish:
                message.SourceNodeId = ReadString(content, ref offset);
                message.MessageId = ReadString(content, ref offset);
                message.Topic = ReadString(content, ref offset);
                var flags = content[offset++];
                message.QoS = (MqttQualityOfService)(flags & 0x03);
                message.Retain = (flags & 0x04) != 0;
                var payloadLength = ReadInt32BigEndian(content[offset..]);
                offset += 4;
                message.Payload = content.Slice(offset, payloadLength).ToArray();
                offset += payloadLength;
                message.Timestamp = ReadInt64BigEndian(content[offset..]);
                break;

            case ClusterMessageType.Subscribe:
            case ClusterMessageType.Unsubscribe:
                message.SourceNodeId = ReadString(content, ref offset);
                message.Topic = ReadString(content, ref offset);
                message.Timestamp = ReadInt64BigEndian(content[offset..]);
                break;

            case ClusterMessageType.HandshakeRequest:
            case ClusterMessageType.HandshakeResponse:
                // 握手消息使用专门的方法处理
                break;

            case ClusterMessageType.NodeLeave:
                message.SourceNodeId = ReadString(content, ref offset);
                message.Timestamp = ReadInt64BigEndian(content[offset..]);
                break;

            case ClusterMessageType.RetainedSyncRequest:
                message.SourceNodeId = ReadString(content, ref offset);
                message.Timestamp = ReadInt64BigEndian(content[offset..]);
                break;

            case ClusterMessageType.RetainedSyncData:
                message.SourceNodeId = ReadString(content, ref offset);
                var retainedPayloadLength = ReadInt32BigEndian(content[offset..]);
                offset += 4;
                message.Payload = content.Slice(offset, retainedPayloadLength).ToArray();
                offset += retainedPayloadLength;
                message.Timestamp = ReadInt64BigEndian(content[offset..]);
                break;
        }

        return message;
    }

    /// <summary>
    /// 序列化握手消息。
    /// </summary>
    /// <param name="handshake">握手数据</param>
    /// <param name="isResponse">是否为响应</param>
    /// <returns>序列化后的字节数组</returns>
    public static byte[] SerializeHandshake(ClusterHandshake handshake, bool isResponse)
    {
        var nodeIdBytes = Encoding.UTF8.GetBytes(handshake.NodeId);
        var clusterNameBytes = Encoding.UTF8.GetBytes(handshake.ClusterName);
        var nodeAddressBytes = string.IsNullOrEmpty(handshake.NodeAddress)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(handshake.NodeAddress);

        var contentSize = 1 + // 协议版本
                         2 + nodeIdBytes.Length +
                         2 + clusterNameBytes.Length +
                         4 + // 监听端口
                         2 + nodeAddressBytes.Length +
                         8; // 时间戳

        var buffer = new byte[HeaderSize + contentSize];
        var span = buffer.AsSpan();

        // 头部
        span[0] = (byte)(isResponse ? ClusterMessageType.HandshakeResponse : ClusterMessageType.HandshakeRequest);
        WriteInt32BigEndian(span[1..], contentSize);

        // 内容
        var offset = HeaderSize;
        span[offset++] = handshake.ProtocolVersion;
        WriteString(span, ref offset, nodeIdBytes);
        WriteString(span, ref offset, clusterNameBytes);
        WriteInt32BigEndian(span[offset..], handshake.ListenPort);
        offset += 4;
        WriteString(span, ref offset, nodeAddressBytes);
        WriteInt64BigEndian(span[offset..], handshake.Timestamp);

        return buffer;
    }

    /// <summary>
    /// 反序列化握手消息。
    /// </summary>
    /// <param name="content">消息内容</param>
    /// <returns>握手数据</returns>
    public static ClusterHandshake DeserializeHandshake(ReadOnlySpan<byte> content)
    {
        var offset = 0;
        var protocolVersion = content[offset++];
        var nodeId = ReadString(content, ref offset);
        var clusterName = ReadString(content, ref offset);
        var listenPort = ReadInt32BigEndian(content[offset..]);
        offset += 4;
        var nodeAddress = ReadString(content, ref offset);
        var timestamp = ReadInt64BigEndian(content[offset..]);

        return new ClusterHandshake
        {
            ProtocolVersion = protocolVersion,
            NodeId = nodeId,
            ClusterName = clusterName,
            ListenPort = listenPort,
            NodeAddress = string.IsNullOrEmpty(nodeAddress) ? null : nodeAddress,
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// 计算消息内容大小。
    /// </summary>
    private static int CalculateContentSize(ClusterMessage message)
    {
        var sourceNodeIdBytes = Encoding.UTF8.GetByteCount(message.SourceNodeId);
        var size = 2 + sourceNodeIdBytes; // SourceNodeId

        switch (message.Type)
        {
            case ClusterMessageType.Heartbeat:
                size += 8; // Timestamp
                break;

            case ClusterMessageType.Publish:
                var messageIdBytes = Encoding.UTF8.GetByteCount(message.MessageId);
                var topicBytes = Encoding.UTF8.GetByteCount(message.Topic ?? string.Empty);
                size += 2 + messageIdBytes; // MessageId
                size += 2 + topicBytes; // Topic
                size += 1; // QoS + Retain 标志
                size += 4 + message.Payload.Length; // Payload
                size += 8; // Timestamp
                break;

            case ClusterMessageType.Subscribe:
            case ClusterMessageType.Unsubscribe:
                var subTopicBytes = Encoding.UTF8.GetByteCount(message.Topic ?? string.Empty);
                size += 2 + subTopicBytes; // Topic
                size += 8; // Timestamp
                break;

            case ClusterMessageType.NodeLeave:
                size += 8; // Timestamp
                break;

            case ClusterMessageType.RetainedSyncRequest:
                size += 8; // Timestamp
                break;

            case ClusterMessageType.RetainedSyncData:
                size += 4 + message.Payload.Length; // Payload
                size += 8; // Timestamp
                break;
        }

        return size;
    }

    /// <summary>
    /// 写入消息内容。
    /// </summary>
    private static void WriteContent(Span<byte> buffer, ref int offset, ClusterMessage message)
    {
        WriteString(buffer, ref offset, message.SourceNodeId);

        switch (message.Type)
        {
            case ClusterMessageType.Heartbeat:
                WriteInt64BigEndian(buffer[offset..], message.Timestamp);
                offset += 8;
                break;

            case ClusterMessageType.Publish:
                WriteString(buffer, ref offset, message.MessageId);
                WriteString(buffer, ref offset, message.Topic ?? string.Empty);
                buffer[offset++] = (byte)((int)message.QoS | (message.Retain ? 0x04 : 0));
                WriteInt32BigEndian(buffer[offset..], message.Payload.Length);
                offset += 4;
                message.Payload.CopyTo(buffer[offset..]);
                offset += message.Payload.Length;
                WriteInt64BigEndian(buffer[offset..], message.Timestamp);
                offset += 8;
                break;

            case ClusterMessageType.Subscribe:
            case ClusterMessageType.Unsubscribe:
                WriteString(buffer, ref offset, message.Topic ?? string.Empty);
                WriteInt64BigEndian(buffer[offset..], message.Timestamp);
                offset += 8;
                break;

            case ClusterMessageType.NodeLeave:
                WriteInt64BigEndian(buffer[offset..], message.Timestamp);
                offset += 8;
                break;

            case ClusterMessageType.RetainedSyncRequest:
                WriteInt64BigEndian(buffer[offset..], message.Timestamp);
                offset += 8;
                break;

            case ClusterMessageType.RetainedSyncData:
                WriteInt32BigEndian(buffer[offset..], message.Payload.Length);
                offset += 4;
                message.Payload.CopyTo(buffer[offset..]);
                offset += message.Payload.Length;
                WriteInt64BigEndian(buffer[offset..], message.Timestamp);
                offset += 8;
                break;
        }
    }

    /// <summary>
    /// 写入字符串。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteString(Span<byte> buffer, ref int offset, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteString(buffer, ref offset, bytes);
    }

    /// <summary>
    /// 写入字符串（字节数组）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteString(Span<byte> buffer, ref int offset, byte[] bytes)
    {
        buffer[offset++] = (byte)(bytes.Length >> 8);
        buffer[offset++] = (byte)bytes.Length;
        bytes.CopyTo(buffer[offset..]);
        offset += bytes.Length;
    }

    /// <summary>
    /// 读取字符串。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadString(ReadOnlySpan<byte> buffer, ref int offset)
    {
        var length = (buffer[offset] << 8) | buffer[offset + 1];
        offset += 2;
        var value = Encoding.UTF8.GetString(buffer.Slice(offset, length));
        offset += length;
        return value;
    }

    /// <summary>
    /// 写入 32 位整数（大端序）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt32BigEndian(Span<byte> buffer, int value)
    {
        buffer[0] = (byte)(value >> 24);
        buffer[1] = (byte)(value >> 16);
        buffer[2] = (byte)(value >> 8);
        buffer[3] = (byte)value;
    }

    /// <summary>
    /// 读取 32 位整数（大端序）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadInt32BigEndian(ReadOnlySpan<byte> buffer)
    {
        return (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
    }

    /// <summary>
    /// 写入 64 位整数（大端序）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteInt64BigEndian(Span<byte> buffer, long value)
    {
        buffer[0] = (byte)(value >> 56);
        buffer[1] = (byte)(value >> 48);
        buffer[2] = (byte)(value >> 40);
        buffer[3] = (byte)(value >> 32);
        buffer[4] = (byte)(value >> 24);
        buffer[5] = (byte)(value >> 16);
        buffer[6] = (byte)(value >> 8);
        buffer[7] = (byte)value;
    }

    /// <summary>
    /// 读取 64 位整数（大端序）。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ReadInt64BigEndian(ReadOnlySpan<byte> buffer)
    {
        return ((long)buffer[0] << 56) |
               ((long)buffer[1] << 48) |
               ((long)buffer[2] << 40) |
               ((long)buffer[3] << 32) |
               ((long)buffer[4] << 24) |
               ((long)buffer[5] << 16) |
               ((long)buffer[6] << 8) |
               buffer[7];
    }
}
