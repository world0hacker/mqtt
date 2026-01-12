namespace System.Net.MQTT;

/// <summary>
/// 收到消息时的事件参数。
/// </summary>
public sealed class MqttMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 获取收到的消息。
    /// </summary>
    public MqttApplicationMessage Message { get; init; } = null!;

    /// <summary>
    /// 获取发送消息的客户端标识符（用于 Broker）。
    /// </summary>
    public string? ClientId { get; init; }
}

/// <summary>
/// 客户端断开连接时的事件参数。
/// </summary>
public sealed class MqttDisconnectedEventArgs : EventArgs
{
    /// <summary>
    /// 获取导致断开连接的异常（如果有）。
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// 获取断开连接是否由客户端发起。
    /// </summary>
    public bool ClientInitiated { get; init; }

    /// <summary>
    /// 获取客户端是否将尝试重新连接。
    /// </summary>
    public bool WillReconnect { get; init; }
}

/// <summary>
/// 客户端连接成功时的事件参数。
/// </summary>
public sealed class MqttConnectedEventArgs : EventArgs
{
    /// <summary>
    /// 获取连接结果。
    /// </summary>
    public MqttConnectResult Result { get; init; } = null!;
}
