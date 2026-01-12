namespace System.Net.MQTT;

/// <summary>
/// 表示主题订阅。
/// </summary>
public sealed class MqttTopicSubscription
{
    /// <summary>
    /// 获取或设置主题过滤器。
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置服务质量级别。
    /// </summary>
    public MqttQualityOfService QualityOfService { get; set; } = MqttQualityOfService.AtMostOnce;

    /// <summary>
    /// 创建新的主题订阅。
    /// </summary>
    /// <param name="topic">主题过滤器</param>
    /// <param name="qos">服务质量级别</param>
    /// <returns>主题订阅实例</returns>
    public static MqttTopicSubscription Create(string topic, MqttQualityOfService qos = MqttQualityOfService.AtMostOnce)
    {
        return new MqttTopicSubscription
        {
            Topic = topic,
            QualityOfService = qos
        };
    }
}

/// <summary>
/// 订阅结果码。
/// </summary>
public enum MqttSubscribeResultCode : byte
{
    /// <summary>授予 QoS 0</summary>
    GrantedQoS0 = 0x00,
    /// <summary>授予 QoS 1</summary>
    GrantedQoS1 = 0x01,
    /// <summary>授予 QoS 2</summary>
    GrantedQoS2 = 0x02,
    /// <summary>订阅失败</summary>
    Failure = 0x80
}

/// <summary>
/// 表示订阅操作的结果。
/// </summary>
public sealed class MqttSubscribeResult
{
    /// <summary>
    /// 获取每个订阅的结果码。
    /// </summary>
    public IReadOnlyList<MqttSubscribeResultCode> Results { get; init; } = Array.Empty<MqttSubscribeResultCode>();

    /// <summary>
    /// 获取所有订阅是否都成功。
    /// </summary>
    public bool IsSuccess => Results.All(r => r != MqttSubscribeResultCode.Failure);
}

/// <summary>
/// 表示取消订阅操作的结果。
/// </summary>
public sealed class MqttUnsubscribeResult
{
    /// <summary>
    /// 获取取消订阅是否成功。
    /// </summary>
    public bool IsSuccess { get; init; }
}
