using System.Buffers;
using System.Net.MQTT.Protocol;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V311;

/// <summary>
/// MQTT 3.1.1 协议处理器实现。
/// 提供 MQTT 3.1.1 版本的所有报文解析和构建功能。
/// </summary>
public sealed class V311ProtocolHandler : IMqttProtocolHandler
{
    // 延迟初始化的解析器和构建器实例
    private readonly Lazy<V311ConnectPacketParser> _connectParser = new(() => new());
    private readonly Lazy<V311ConnectPacketBuilder> _connectBuilder = new(() => new());
    private readonly Lazy<V311ConnAckPacketParser> _connAckParser = new(() => new());
    private readonly Lazy<V311ConnAckPacketBuilder> _connAckBuilder = new(() => new());
    private readonly Lazy<V311PublishPacketParser> _publishParser = new(() => new());
    private readonly Lazy<V311PublishPacketBuilder> _publishBuilder = new(() => new());
    private readonly Lazy<V311PubAckPacketParser> _pubAckParser = new(() => new());
    private readonly Lazy<V311PubAckPacketBuilder> _pubAckBuilder = new(() => new());
    private readonly Lazy<V311SubscribePacketParser> _subscribeParser = new(() => new());
    private readonly Lazy<V311SubscribePacketBuilder> _subscribeBuilder = new(() => new());
    private readonly Lazy<V311SubAckPacketParser> _subAckParser = new(() => new());
    private readonly Lazy<V311SubAckPacketBuilder> _subAckBuilder = new(() => new());
    private readonly Lazy<V311UnsubscribePacketParser> _unsubscribeParser = new(() => new());
    private readonly Lazy<V311UnsubscribePacketBuilder> _unsubscribeBuilder = new(() => new());
    private readonly Lazy<V311UnsubAckPacketParser> _unsubAckParser = new(() => new());
    private readonly Lazy<V311UnsubAckPacketBuilder> _unsubAckBuilder = new(() => new());
    private readonly Lazy<V311DisconnectPacketParser> _disconnectParser = new(() => new());
    private readonly Lazy<V311DisconnectPacketBuilder> _disconnectBuilder = new(() => new());

    /// <inheritdoc/>
    public MqttProtocolVersion ProtocolVersion => MqttProtocolVersion.V311;

    /// <inheritdoc/>
    public bool SupportsAuth => false;

    /// <inheritdoc/>
    public bool SupportsProperties => false;

    #region 解析器

    /// <inheritdoc/>
    public IConnectPacketParser ConnectParser => _connectParser.Value;

    /// <inheritdoc/>
    public IConnAckPacketParser ConnAckParser => _connAckParser.Value;

    /// <inheritdoc/>
    public IPublishPacketParser PublishParser => _publishParser.Value;

    /// <inheritdoc/>
    public IPubAckPacketParser PubAckParser => _pubAckParser.Value;

    /// <inheritdoc/>
    public ISubscribePacketParser SubscribeParser => _subscribeParser.Value;

    /// <inheritdoc/>
    public ISubAckPacketParser SubAckParser => _subAckParser.Value;

    /// <inheritdoc/>
    public IUnsubscribePacketParser UnsubscribeParser => _unsubscribeParser.Value;

    /// <inheritdoc/>
    public IUnsubAckPacketParser UnsubAckParser => _unsubAckParser.Value;

    /// <inheritdoc/>
    public IDisconnectPacketParser DisconnectParser => _disconnectParser.Value;

    /// <inheritdoc/>
    public IAuthPacketParser? AuthParser => null; // MQTT 3.1.1 不支持 AUTH

    #endregion

    #region 构建器

    /// <inheritdoc/>
    public IConnectPacketBuilder ConnectBuilder => _connectBuilder.Value;

    /// <inheritdoc/>
    public IConnAckPacketBuilder ConnAckBuilder => _connAckBuilder.Value;

    /// <inheritdoc/>
    public IPublishPacketBuilder PublishBuilder => _publishBuilder.Value;

    /// <inheritdoc/>
    public IPubAckPacketBuilder PubAckBuilder => _pubAckBuilder.Value;

    /// <inheritdoc/>
    public ISubscribePacketBuilder SubscribeBuilder => _subscribeBuilder.Value;

    /// <inheritdoc/>
    public ISubAckPacketBuilder SubAckBuilder => _subAckBuilder.Value;

    /// <inheritdoc/>
    public IUnsubscribePacketBuilder UnsubscribeBuilder => _unsubscribeBuilder.Value;

    /// <inheritdoc/>
    public IUnsubAckPacketBuilder UnsubAckBuilder => _unsubAckBuilder.Value;

    /// <inheritdoc/>
    public IDisconnectPacketBuilder DisconnectBuilder => _disconnectBuilder.Value;

    /// <inheritdoc/>
    public IAuthPacketBuilder? AuthBuilder => null; // MQTT 3.1.1 不支持 AUTH

    /// <inheritdoc/>
    public IPingPacketHandler PingHandler => MqttPingPacketHandler.Instance;

    #endregion

    #region 高层方法

    /// <inheritdoc/>
    public IMqttPacket ParsePacket(MqttPacketType packetType, byte flags, ReadOnlySequence<byte> data)
    {
        return packetType switch
        {
            MqttPacketType.Connect => ConnectParser.Parse(data, flags),
            MqttPacketType.ConnAck => ConnAckParser.Parse(data, flags),
            MqttPacketType.Publish => PublishParser.Parse(data, flags),
            MqttPacketType.PubAck => ParsePubAckPacket(MqttPacketType.PubAck, data, flags),
            MqttPacketType.PubRec => ParsePubAckPacket(MqttPacketType.PubRec, data, flags),
            MqttPacketType.PubRel => ParsePubAckPacket(MqttPacketType.PubRel, data, flags),
            MqttPacketType.PubComp => ParsePubAckPacket(MqttPacketType.PubComp, data, flags),
            MqttPacketType.Subscribe => SubscribeParser.Parse(data, flags),
            MqttPacketType.SubAck => SubAckParser.Parse(data, flags),
            MqttPacketType.Unsubscribe => UnsubscribeParser.Parse(data, flags),
            MqttPacketType.UnsubAck => UnsubAckParser.Parse(data, flags),
            MqttPacketType.PingReq => MqttPingReqPacket.Instance,
            MqttPacketType.PingResp => MqttPingRespPacket.Instance,
            MqttPacketType.Disconnect => DisconnectParser.Parse(data, flags),
            _ => throw new MqttProtocolException($"不支持的报文类型: {packetType}")
        };
    }

    /// <inheritdoc/>
    public IMqttPacket ParsePacket(MqttPacketType packetType, byte flags, ReadOnlySpan<byte> data)
    {
        return packetType switch
        {
            MqttPacketType.Connect => ConnectParser.Parse(data, flags),
            MqttPacketType.ConnAck => ConnAckParser.Parse(data, flags),
            MqttPacketType.Publish => PublishParser.Parse(data, flags),
            MqttPacketType.PubAck => ParsePubAckPacket(MqttPacketType.PubAck, data, flags),
            MqttPacketType.PubRec => ParsePubAckPacket(MqttPacketType.PubRec, data, flags),
            MqttPacketType.PubRel => ParsePubAckPacket(MqttPacketType.PubRel, data, flags),
            MqttPacketType.PubComp => ParsePubAckPacket(MqttPacketType.PubComp, data, flags),
            MqttPacketType.Subscribe => SubscribeParser.Parse(data, flags),
            MqttPacketType.SubAck => SubAckParser.Parse(data, flags),
            MqttPacketType.Unsubscribe => UnsubscribeParser.Parse(data, flags),
            MqttPacketType.UnsubAck => UnsubAckParser.Parse(data, flags),
            MqttPacketType.PingReq => MqttPingReqPacket.Instance,
            MqttPacketType.PingResp => MqttPingRespPacket.Instance,
            MqttPacketType.Disconnect => DisconnectParser.Parse(data, flags),
            _ => throw new MqttProtocolException($"不支持的报文类型: {packetType}")
        };
    }

    /// <summary>
    /// 解析 PUBACK/PUBREC/PUBREL/PUBCOMP 报文。
    /// </summary>
    private MqttPubAckPacket ParsePubAckPacket(MqttPacketType type, ReadOnlySequence<byte> data, byte flags)
    {
        var packet = PubAckParser.Parse(data, flags);
        packet.PacketType = type;
        return packet;
    }

    /// <summary>
    /// 解析 PUBACK/PUBREC/PUBREL/PUBCOMP 报文（Span 版本）。
    /// </summary>
    private MqttPubAckPacket ParsePubAckPacket(MqttPacketType type, ReadOnlySpan<byte> data, byte flags)
    {
        var packet = PubAckParser.Parse(data, flags);
        packet.PacketType = type;
        return packet;
    }

    /// <inheritdoc/>
    public void WritePacket(IMqttPacket packet, IBufferWriter<byte> writer)
    {
        switch (packet)
        {
            case MqttConnectPacket connectPacket:
                ConnectBuilder.WriteTo(connectPacket, writer);
                break;
            case MqttConnAckPacket connAckPacket:
                ConnAckBuilder.WriteTo(connAckPacket, writer);
                break;
            case MqttPublishPacket publishPacket:
                PublishBuilder.WriteTo(publishPacket, writer);
                break;
            case MqttPubAckPacket pubAckPacket:
                PubAckBuilder.WriteTo(pubAckPacket, writer);
                break;
            case MqttSubscribePacket subscribePacket:
                SubscribeBuilder.WriteTo(subscribePacket, writer);
                break;
            case MqttSubAckPacket subAckPacket:
                SubAckBuilder.WriteTo(subAckPacket, writer);
                break;
            case MqttUnsubscribePacket unsubscribePacket:
                UnsubscribeBuilder.WriteTo(unsubscribePacket, writer);
                break;
            case MqttUnsubAckPacket unsubAckPacket:
                UnsubAckBuilder.WriteTo(unsubAckPacket, writer);
                break;
            case MqttPingReqPacket:
                WritePingReq(writer);
                break;
            case MqttPingRespPacket:
                WritePingResp(writer);
                break;
            case MqttDisconnectPacket disconnectPacket:
                DisconnectBuilder.WriteTo(disconnectPacket, writer);
                break;
            default:
                throw new MqttProtocolException($"不支持的报文类型: {packet.GetType().Name}");
        }
    }

    /// <summary>
    /// 写入 PINGREQ 报文。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePingReq(IBufferWriter<byte> writer)
    {
        var span = writer.GetSpan(2);
        PingHandler.WritePingReq(span);
        writer.Advance(2);
    }

    /// <summary>
    /// 写入 PINGRESP 报文。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePingResp(IBufferWriter<byte> writer)
    {
        var span = writer.GetSpan(2);
        PingHandler.WritePingResp(span);
        writer.Advance(2);
    }

    /// <inheritdoc/>
    public async ValueTask<IMqttPacket?> ReadPacketAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        // 读取固定头部第一个字节
        var headerBuffer = new byte[1];
        var bytesRead = await stream.ReadAsync(headerBuffer, cancellationToken).ConfigureAwait(false);
        if (bytesRead == 0) return null; // 连接关闭

        var packetType = (MqttPacketType)(headerBuffer[0] >> 4);
        var flags = (byte)(headerBuffer[0] & 0x0F);

        // 读取剩余长度
        uint remainingLength = 0;
        int multiplier = 1;
        byte encodedByte;
        int lengthBytesRead = 0;

        do
        {
            bytesRead = await stream.ReadAsync(headerBuffer, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0) return null;

            encodedByte = headerBuffer[0];
            remainingLength += (uint)((encodedByte & 0x7F) * multiplier);
            multiplier *= 128;
            lengthBytesRead++;

            if (lengthBytesRead > 4)
            {
                throw new MqttProtocolException("剩余长度编码无效");
            }
        } while ((encodedByte & 0x80) != 0);

        // 读取报文内容
        if (remainingLength == 0)
        {
            return ParsePacket(packetType, flags, ReadOnlySpan<byte>.Empty);
        }

        var buffer = ArrayPool<byte>.Shared.Rent((int)remainingLength);
        try
        {
            var totalRead = 0;
            while (totalRead < remainingLength)
            {
                bytesRead = await stream.ReadAsync(
                    buffer.AsMemory(totalRead, (int)remainingLength - totalRead),
                    cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    throw new MqttProtocolException("连接意外关闭");
                }
                totalRead += bytesRead;
            }

            return ParsePacket(packetType, flags, buffer.AsSpan(0, (int)remainingLength));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <inheritdoc/>
    public async ValueTask WritePacketAsync(IMqttPacket packet, Stream stream, CancellationToken cancellationToken = default)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        WritePacket(packet, bufferWriter);

        await stream.WriteAsync(bufferWriter.WrittenMemory, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
