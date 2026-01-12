using System.Buffers;
using System.Net.MQTT.Broker.Transport;
using System.Text;

namespace System.Net.MQTT.Broker.Protocol;

/// <summary>
/// MQTT CONNECT 报文解析器实现。
/// </summary>
public sealed class MqttConnectPacketParser : IMqttConnectPacketParser
{
    private readonly int _maxMessageSize;

    /// <summary>
    /// 初始化 MQTT CONNECT 报文解析器。
    /// </summary>
    /// <param name="maxMessageSize">最大消息大小（字节）。</param>
    public MqttConnectPacketParser(int maxMessageSize)
    {
        _maxMessageSize = maxMessageSize;
    }

    /// <inheritdoc/>
    public async ValueTask<MqttConnectResult?> ParseAsync(
        ITransportConnection connection,
        CancellationToken cancellationToken)
    {
        // 从池中租借缓冲区读取固定头部
        var headerBuffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            await ReadExactlyAsync(connection, headerBuffer, 0, 1, cancellationToken);

            // 验证报文类型是否为 CONNECT
            if ((headerBuffer[0] >> 4) != (int)MqttPacketType.Connect)
            {
                return null;
            }

            // 异步解码剩余长度
            var remainingLength = await DecodeRemainingLengthAsync(connection, cancellationToken);

            // 检查 CONNECT 报文大小
            if (remainingLength > _maxMessageSize)
            {
                // CONNECT 报文过大，拒绝连接
                return null;
            }

            // 从池中租借有效载荷缓冲区
            var payload = ArrayPool<byte>.Shared.Rent(remainingLength);
            try
            {
                await ReadExactlyAsync(connection, payload, 0, remainingLength, cancellationToken);

                // 解析 CONNECT 报文内容
                return ParseConnectPayload(payload, remainingLength);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(payload);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    private static MqttConnectResult ParseConnectPayload(byte[] payload, int length)
    {
        var index = 0;

        // 协议名称长度
        var protocolNameLength = (payload[index] << 8) | payload[index + 1];
        index += 2;

        // 协议名称
        var protocolName = Encoding.UTF8.GetString(payload, index, protocolNameLength);
        index += protocolNameLength;

        // 协议版本
        var protocolVersion = (MqttProtocolVersion)payload[index++];

        // 连接标志
        var connectFlags = payload[index++];

        // 保活时间（大端序）
        var keepAlive = (ushort)((payload[index] << 8) | payload[index + 1]);
        index += 2;

        // MQTT 5.0: 跳过 CONNECT 属性
        if (protocolVersion == MqttProtocolVersion.V500)
        {
            // 读取可变字节整数（属性长度）
            var propertiesLength = 0;
            var multiplier = 1;
            byte encodedByte;
            do
            {
                encodedByte = payload[index++];
                propertiesLength += (encodedByte & 127) * multiplier;
                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

            // 跳过属性数据
            index += propertiesLength;
        }

        // 解析连接标志位
        var cleanSession = (connectFlags & 0x02) != 0;
        var hasWill = (connectFlags & 0x04) != 0;
        var willQos = (MqttQualityOfService)((connectFlags >> 3) & 0x03);
        var willRetain = (connectFlags & 0x20) != 0;
        var hasPassword = (connectFlags & 0x40) != 0;
        var hasUsername = (connectFlags & 0x80) != 0;

        // 客户端 ID
        var clientIdLength = (payload[index] << 8) | payload[index + 1];
        index += 2;
        var clientId = Encoding.UTF8.GetString(payload, index, clientIdLength);
        index += clientIdLength;

        // 如果客户端 ID 为空，生成一个唯一 ID
        if (string.IsNullOrEmpty(clientId))
        {
            clientId = Guid.NewGuid().ToString("N");
        }

        // 遗嘱消息
        MqttApplicationMessage? willMessage = null;
        if (hasWill)
        {
            // MQTT 5.0: 跳过遗嘱属性
            if (protocolVersion == MqttProtocolVersion.V500)
            {
                var willPropsLength = 0;
                var multiplier = 1;
                byte encodedByte;
                do
                {
                    encodedByte = payload[index++];
                    willPropsLength += (encodedByte & 127) * multiplier;
                    multiplier *= 128;
                } while ((encodedByte & 128) != 0);
                index += willPropsLength;
            }

            var willTopicLength = (payload[index] << 8) | payload[index + 1];
            index += 2;
            var willTopic = Encoding.UTF8.GetString(payload, index, willTopicLength);
            index += willTopicLength;

            var willPayloadLength = (payload[index] << 8) | payload[index + 1];
            index += 2;
            var willPayload = new byte[willPayloadLength];
            payload.AsSpan(index, willPayloadLength).CopyTo(willPayload);
            index += willPayloadLength;

            willMessage = new MqttApplicationMessage
            {
                Topic = willTopic,
                Payload = willPayload,
                QualityOfService = willQos,
                Retain = willRetain
            };
        }

        // 用户名和密码
        string? username = null;
        string? password = null;

        if (hasUsername)
        {
            var usernameLength = (payload[index] << 8) | payload[index + 1];
            index += 2;
            username = Encoding.UTF8.GetString(payload, index, usernameLength);
            index += usernameLength;
        }

        if (hasPassword)
        {
            var passwordLength = (payload[index] << 8) | payload[index + 1];
            index += 2;
            password = Encoding.UTF8.GetString(payload, index, passwordLength);
        }

        return new MqttConnectResult
        {
            ProtocolVersion = protocolVersion,
            ClientId = clientId,
            CleanSession = cleanSession,
            KeepAlive = keepAlive,
            WillMessage = willMessage,
            Username = username,
            Password = password
        };
    }

    private static async ValueTask ReadExactlyAsync(
        ITransportConnection connection,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await connection.ReadAsync(buffer.AsMemory(offset + totalRead, count - totalRead), cancellationToken);
            if (read == 0)
            {
                throw new IOException("连接已关闭");
            }
            totalRead += read;
        }
    }

    private static async ValueTask<int> DecodeRemainingLengthAsync(
        ITransportConnection connection,
        CancellationToken cancellationToken)
    {
        var multiplier = 1;
        var value = 0;
        var buffer = new byte[1];

        do
        {
            await ReadExactlyAsync(connection, buffer, 0, 1, cancellationToken);
            var encodedByte = buffer[0];
            value += (encodedByte & 127) * multiplier;
            multiplier *= 128;

            if (multiplier > 128 * 128 * 128)
            {
                throw new InvalidDataException("剩余长度格式无效");
            }
        } while ((buffer[0] & 128) != 0);

        return value;
    }
}
