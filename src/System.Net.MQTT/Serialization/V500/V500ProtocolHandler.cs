using System.Buffers;
using System.Net.MQTT.Protocol;
using System.Net.MQTT.Protocol.Packets;
using System.Net.MQTT.Protocol.Properties;
using System.Net.MQTT.Serialization.Common;
using System.Net.MQTT.Serialization.Interfaces;
using System.Runtime.CompilerServices;

namespace System.Net.MQTT.Serialization.V500;

/// <summary>
/// MQTT 5.0 协议处理器实现。
/// 提供 MQTT 5.0 版本的所有报文解析和构建功能，包括属性支持。
/// </summary>
public sealed class V500ProtocolHandler : IMqttProtocolHandler
{
    // 属性解析器和构建器
    private readonly V500PropertyParser _propertyParser = new();
    private readonly V500PropertyBuilder _propertyBuilder = new();

    // 延迟初始化的解析器和构建器实例
    private readonly Lazy<V500ConnectPacketParser> _connectParser;
    private readonly Lazy<V500ConnectPacketBuilder> _connectBuilder;
    private readonly Lazy<V500ConnAckPacketParser> _connAckParser;
    private readonly Lazy<V500ConnAckPacketBuilder> _connAckBuilder;
    private readonly Lazy<V500PublishPacketParser> _publishParser;
    private readonly Lazy<V500PublishPacketBuilder> _publishBuilder;
    private readonly Lazy<V500PubAckPacketParser> _pubAckParser;
    private readonly Lazy<V500PubAckPacketBuilder> _pubAckBuilder;
    private readonly Lazy<V500SubscribePacketParser> _subscribeParser;
    private readonly Lazy<V500SubscribePacketBuilder> _subscribeBuilder;
    private readonly Lazy<V500SubAckPacketParser> _subAckParser;
    private readonly Lazy<V500SubAckPacketBuilder> _subAckBuilder;
    private readonly Lazy<V500UnsubscribePacketParser> _unsubscribeParser;
    private readonly Lazy<V500UnsubscribePacketBuilder> _unsubscribeBuilder;
    private readonly Lazy<V500UnsubAckPacketParser> _unsubAckParser;
    private readonly Lazy<V500UnsubAckPacketBuilder> _unsubAckBuilder;
    private readonly Lazy<V500DisconnectPacketParser> _disconnectParser;
    private readonly Lazy<V500DisconnectPacketBuilder> _disconnectBuilder;
    private readonly Lazy<V500AuthPacketParser> _authParser;
    private readonly Lazy<V500AuthPacketBuilder> _authBuilder;

    /// <summary>
    /// 创建 MQTT 5.0 协议处理器。
    /// </summary>
    public V500ProtocolHandler()
    {
        _connectParser = new(() => new V500ConnectPacketParser(_propertyParser));
        _connectBuilder = new(() => new V500ConnectPacketBuilder(_propertyBuilder));
        _connAckParser = new(() => new V500ConnAckPacketParser(_propertyParser));
        _connAckBuilder = new(() => new V500ConnAckPacketBuilder(_propertyBuilder));
        _publishParser = new(() => new V500PublishPacketParser(_propertyParser));
        _publishBuilder = new(() => new V500PublishPacketBuilder(_propertyBuilder));
        _pubAckParser = new(() => new V500PubAckPacketParser(_propertyParser));
        _pubAckBuilder = new(() => new V500PubAckPacketBuilder(_propertyBuilder));
        _subscribeParser = new(() => new V500SubscribePacketParser(_propertyParser));
        _subscribeBuilder = new(() => new V500SubscribePacketBuilder(_propertyBuilder));
        _subAckParser = new(() => new V500SubAckPacketParser(_propertyParser));
        _subAckBuilder = new(() => new V500SubAckPacketBuilder(_propertyBuilder));
        _unsubscribeParser = new(() => new V500UnsubscribePacketParser(_propertyParser));
        _unsubscribeBuilder = new(() => new V500UnsubscribePacketBuilder(_propertyBuilder));
        _unsubAckParser = new(() => new V500UnsubAckPacketParser(_propertyParser));
        _unsubAckBuilder = new(() => new V500UnsubAckPacketBuilder(_propertyBuilder));
        _disconnectParser = new(() => new V500DisconnectPacketParser(_propertyParser));
        _disconnectBuilder = new(() => new V500DisconnectPacketBuilder(_propertyBuilder));
        _authParser = new(() => new V500AuthPacketParser(_propertyParser));
        _authBuilder = new(() => new V500AuthPacketBuilder(_propertyBuilder));
    }

    /// <inheritdoc/>
    public MqttProtocolVersion ProtocolVersion => MqttProtocolVersion.V500;

    /// <inheritdoc/>
    public bool SupportsAuth => true;

    /// <inheritdoc/>
    public bool SupportsProperties => true;

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
    public IAuthPacketParser? AuthParser => _authParser.Value;

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
    public IAuthPacketBuilder? AuthBuilder => _authBuilder.Value;

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
            MqttPacketType.Auth => AuthParser!.Parse(data, flags),
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
            MqttPacketType.Auth => AuthParser!.Parse(data, flags),
            _ => throw new MqttProtocolException($"不支持的报文类型: {packetType}")
        };
    }

    private MqttPubAckPacket ParsePubAckPacket(MqttPacketType type, ReadOnlySequence<byte> data, byte flags)
    {
        var packet = PubAckParser.Parse(data, flags);
        packet.PacketType = type;
        return packet;
    }

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
            case MqttAuthPacket authPacket:
                AuthBuilder!.WriteTo(authPacket, writer);
                break;
            default:
                throw new MqttProtocolException($"不支持的报文类型: {packet.GetType().Name}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WritePingReq(IBufferWriter<byte> writer)
    {
        var span = writer.GetSpan(2);
        PingHandler.WritePingReq(span);
        writer.Advance(2);
    }

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
        var headerBuffer = new byte[1];
        var bytesRead = await stream.ReadAsync(headerBuffer, cancellationToken).ConfigureAwait(false);
        if (bytesRead == 0) return null;

        var packetType = (MqttPacketType)(headerBuffer[0] >> 4);
        var flags = (byte)(headerBuffer[0] & 0x0F);

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
