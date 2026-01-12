namespace System.Net.MQTT;

/// <summary>
/// MQTT 客户端接口。
/// </summary>
public interface IMqttClient : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// 获取客户端是否已连接。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 获取客户端配置选项。
    /// </summary>
    MqttClientOptions Options { get; }

    /// <summary>
    /// 当收到消息时触发。
    /// </summary>
    event EventHandler<MqttMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// 当客户端连接成功时触发。
    /// </summary>
    event EventHandler<MqttConnectedEventArgs>? Connected;

    /// <summary>
    /// 当客户端断开连接时触发。
    /// </summary>
    event EventHandler<MqttDisconnectedEventArgs>? Disconnected;

    /// <summary>
    /// 连接到 MQTT 服务器。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接结果</returns>
    Task<MqttConnectResult> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开与 MQTT 服务器的连接。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布消息到指定主题。
    /// </summary>
    /// <param name="message">要发布的消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PublishAsync(MqttApplicationMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布字符串消息到指定主题。
    /// </summary>
    /// <param name="topic">主题</param>
    /// <param name="payload">消息内容</param>
    /// <param name="qos">服务质量级别</param>
    /// <param name="retain">是否保留消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PublishAsync(string topic, string payload, MqttQualityOfService qos = MqttQualityOfService.AtMostOnce, bool retain = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅多个主题。
    /// </summary>
    /// <param name="subscriptions">订阅列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>订阅结果</returns>
    Task<MqttSubscribeResult> SubscribeAsync(IEnumerable<MqttTopicSubscription> subscriptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// 订阅单个主题。
    /// </summary>
    /// <param name="topic">主题</param>
    /// <param name="qos">服务质量级别</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>订阅结果</returns>
    Task<MqttSubscribeResult> SubscribeAsync(string topic, MqttQualityOfService qos = MqttQualityOfService.AtMostOnce, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消订阅多个主题。
    /// </summary>
    /// <param name="topics">主题列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>取消订阅结果</returns>
    Task<MqttUnsubscribeResult> UnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消订阅单个主题。
    /// </summary>
    /// <param name="topic">主题</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>取消订阅结果</returns>
    Task<MqttUnsubscribeResult> UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);
}
