namespace System.Net.MQTT.Broker.Bridge;

/// <summary>
/// MQTT 桥接异常。
/// </summary>
public class MqttBridgeException : Exception
{
    /// <summary>
    /// 获取桥接名称。
    /// </summary>
    public string? BridgeName { get; }

    /// <summary>
    /// 初始化 <see cref="MqttBridgeException"/> 类的新实例。
    /// </summary>
    public MqttBridgeException()
    {
    }

    /// <summary>
    /// 使用指定的错误消息初始化 <see cref="MqttBridgeException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息</param>
    public MqttBridgeException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定的错误消息和桥接名称初始化 <see cref="MqttBridgeException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="bridgeName">桥接名称</param>
    public MqttBridgeException(string message, string bridgeName) : base(message)
    {
        BridgeName = bridgeName;
    }

    /// <summary>
    /// 使用指定的错误消息和内部异常初始化 <see cref="MqttBridgeException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public MqttBridgeException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// 使用指定的错误消息、桥接名称和内部异常初始化 <see cref="MqttBridgeException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="bridgeName">桥接名称</param>
    /// <param name="innerException">内部异常</param>
    public MqttBridgeException(string message, string bridgeName, Exception innerException) : base(message, innerException)
    {
        BridgeName = bridgeName;
    }
}

/// <summary>
/// MQTT 桥接连接异常。
/// </summary>
public class MqttBridgeConnectionException : MqttBridgeException
{
    /// <summary>
    /// 获取远程端点地址。
    /// </summary>
    public string? RemoteEndpoint { get; }

    /// <summary>
    /// 初始化 <see cref="MqttBridgeConnectionException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="remoteEndpoint">远程端点地址</param>
    public MqttBridgeConnectionException(string message, string remoteEndpoint) : base(message)
    {
        RemoteEndpoint = remoteEndpoint;
    }

    /// <summary>
    /// 初始化 <see cref="MqttBridgeConnectionException"/> 类的新实例。
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="remoteEndpoint">远程端点地址</param>
    /// <param name="innerException">内部异常</param>
    public MqttBridgeConnectionException(string message, string remoteEndpoint, Exception innerException)
        : base(message, innerException)
    {
        RemoteEndpoint = remoteEndpoint;
    }
}
