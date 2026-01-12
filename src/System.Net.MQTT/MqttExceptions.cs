namespace System.Net.MQTT;

/// <summary>
/// 当发生 MQTT 协议错误时抛出的异常。
/// </summary>
public class MqttProtocolException : Exception
{
    /// <summary>
    /// 创建 MqttProtocolException 的新实例。
    /// </summary>
    public MqttProtocolException()
    {
    }

    /// <summary>
    /// 使用指定消息创建 MqttProtocolException 的新实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public MqttProtocolException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定消息和内部异常创建 MqttProtocolException 的新实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public MqttProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// 当发生 MQTT 连接错误时抛出的异常。
/// </summary>
public class MqttConnectionException : MqttProtocolException
{
    /// <summary>
    /// 获取连接结果码。
    /// </summary>
    public MqttConnectResultCode ResultCode { get; }

    /// <summary>
    /// 使用指定结果码创建 MqttConnectionException 的新实例。
    /// </summary>
    /// <param name="resultCode">连接结果码</param>
    public MqttConnectionException(MqttConnectResultCode resultCode)
        : base($"连接失败，结果码: {resultCode}")
    {
        ResultCode = resultCode;
    }

    /// <summary>
    /// 使用指定消息创建 MqttConnectionException 的新实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    public MqttConnectionException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用指定消息和内部异常创建 MqttConnectionException 的新实例。
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public MqttConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
