using System.Threading.Channels;

namespace System.Net.MQTT.Broker;

/// <summary>
/// 事件类型标识。
/// </summary>
internal enum BrokerEventType
{
    ClientConnected,
    ClientDisconnected,
    MessagePublishing,
    MessagePublished,
    MessageNotDelivered,
    MessageDelivered,
    ClientSubscribing,
    ClientSubscribed,
    ClientUnsubscribing,
    ClientUnsubscribed
}

/// <summary>
/// 事件包装器基类。
/// </summary>
internal abstract class BrokerEvent
{
    public abstract BrokerEventType EventType { get; }
}

/// <summary>
/// 泛型事件包装器。
/// </summary>
internal sealed class BrokerEvent<TEventArgs> : BrokerEvent where TEventArgs : EventArgs
{
    public override BrokerEventType EventType { get; }
    public TEventArgs Args { get; }
    public EventHandler<TEventArgs>? Handler { get; }

    public BrokerEvent(BrokerEventType eventType, TEventArgs args, EventHandler<TEventArgs>? handler)
    {
        EventType = eventType;
        Args = args;
        Handler = handler;
    }
}

/// <summary>
/// MQTT Broker 异步事件分发器。
/// 使用 Channel 队列实现非阻塞事件分发，避免事件处理器阻塞消息处理流程。
/// </summary>
internal sealed class MqttBrokerEventDispatcher : IAsyncDisposable
{
    private readonly Channel<BrokerEvent> _eventChannel;
    private readonly Task _dispatchTask;
    private readonly CancellationTokenSource _cts;
    private readonly object _sender;
    private bool _disposed;

    /// <summary>
    /// 获取或设置事件处理异常时的回调。
    /// </summary>
    public Action<Exception, BrokerEvent>? OnEventError { get; set; }

    /// <summary>
    /// 初始化事件分发器。
    /// </summary>
    /// <param name="sender">事件发送者（通常是 MqttBroker）</param>
    /// <param name="capacity">事件队列容量，默认 10000</param>
    public MqttBrokerEventDispatcher(object sender, int capacity = 10000)
    {
        _sender = sender;
        _cts = new CancellationTokenSource();

        // 使用有界通道，避免内存无限增长
        _eventChannel = Channel.CreateBounded<BrokerEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // 队列满时丢弃最旧的事件
            SingleReader = true,
            SingleWriter = false
        });

        _dispatchTask = DispatchEventsAsync(_cts.Token);
    }

    /// <summary>
    /// 发送事件到队列（非阻塞）。
    /// </summary>
    public void Dispatch<TEventArgs>(BrokerEventType eventType, TEventArgs args, EventHandler<TEventArgs>? handler)
        where TEventArgs : EventArgs
    {
        if (_disposed || handler == null) return;

        // TryWrite 是非阻塞的
        _eventChannel.Writer.TryWrite(new BrokerEvent<TEventArgs>(eventType, args, handler));
    }

    /// <summary>
    /// 后台事件消费任务。
    /// </summary>
    private async Task DispatchEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var evt in _eventChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    InvokeEvent(evt);
                }
                catch (Exception ex)
                {
                    OnEventError?.Invoke(ex, evt);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消
        }
    }

    /// <summary>
    /// 调用事件处理器。
    /// </summary>
    private void InvokeEvent(BrokerEvent evt)
    {
        switch (evt)
        {
            case BrokerEvent<MqttClientConnectedEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttClientDisconnectedEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttMessagePublishingEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttMessagePublishedEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttMessageNotDeliveredEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttMessageDeliveredEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttClientSubscribingEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttClientSubscribedEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttClientUnsubscribingEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
            case BrokerEvent<MqttClientUnsubscribedEventArgs> e:
                e.Handler?.Invoke(_sender, e.Args);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _eventChannel.Writer.Complete();
        _cts.Cancel();

        try
        {
            await _dispatchTask;
        }
        catch (OperationCanceledException)
        {
            // 正常
        }

        _cts.Dispose();
    }
}
