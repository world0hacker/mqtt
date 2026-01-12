namespace System.Net.MQTT.Protocol.ReasonCodes;

/// <summary>
/// MQTT 5.0 通用原因码。
/// 定义了所有报文类型中使用的原因码。
/// </summary>
public static class MqttReasonCode
{
    /// <summary>成功 / 正常断开 / 授予 QoS 0</summary>
    public const byte Success = 0x00;

    /// <summary>授予 QoS 1</summary>
    public const byte GrantedQoS1 = 0x01;

    /// <summary>授予 QoS 2</summary>
    public const byte GrantedQoS2 = 0x02;

    /// <summary>带遗嘱消息断开</summary>
    public const byte DisconnectWithWillMessage = 0x04;

    /// <summary>无匹配的订阅者</summary>
    public const byte NoMatchingSubscribers = 0x10;

    /// <summary>无订阅存在</summary>
    public const byte NoSubscriptionExisted = 0x11;

    /// <summary>继续认证</summary>
    public const byte ContinueAuthentication = 0x18;

    /// <summary>重新认证</summary>
    public const byte ReAuthenticate = 0x19;

    /// <summary>未指定错误</summary>
    public const byte UnspecifiedError = 0x80;

    /// <summary>畸形报文</summary>
    public const byte MalformedPacket = 0x81;

    /// <summary>协议错误</summary>
    public const byte ProtocolError = 0x82;

    /// <summary>实现特定错误</summary>
    public const byte ImplementationSpecificError = 0x83;

    /// <summary>不支持的协议版本</summary>
    public const byte UnsupportedProtocolVersion = 0x84;

    /// <summary>客户端标识符无效</summary>
    public const byte ClientIdentifierNotValid = 0x85;

    /// <summary>用户名或密码错误</summary>
    public const byte BadUserNameOrPassword = 0x86;

    /// <summary>未授权</summary>
    public const byte NotAuthorized = 0x87;

    /// <summary>服务器不可用</summary>
    public const byte ServerUnavailable = 0x88;

    /// <summary>服务器繁忙</summary>
    public const byte ServerBusy = 0x89;

    /// <summary>禁止访问</summary>
    public const byte Banned = 0x8A;

    /// <summary>服务器关闭中</summary>
    public const byte ServerShuttingDown = 0x8B;

    /// <summary>错误的认证方法</summary>
    public const byte BadAuthenticationMethod = 0x8C;

    /// <summary>保活超时</summary>
    public const byte KeepAliveTimeout = 0x8D;

    /// <summary>会话被接管</summary>
    public const byte SessionTakenOver = 0x8E;

    /// <summary>主题过滤器无效</summary>
    public const byte TopicFilterInvalid = 0x8F;

    /// <summary>主题名无效</summary>
    public const byte TopicNameInvalid = 0x90;

    /// <summary>报文标识符已被使用</summary>
    public const byte PacketIdentifierInUse = 0x91;

    /// <summary>报文标识符未找到</summary>
    public const byte PacketIdentifierNotFound = 0x92;

    /// <summary>接收最大值超限</summary>
    public const byte ReceiveMaximumExceeded = 0x93;

    /// <summary>主题别名无效</summary>
    public const byte TopicAliasInvalid = 0x94;

    /// <summary>报文过大</summary>
    public const byte PacketTooLarge = 0x95;

    /// <summary>消息速率过高</summary>
    public const byte MessageRateTooHigh = 0x96;

    /// <summary>超出配额</summary>
    public const byte QuotaExceeded = 0x97;

    /// <summary>管理操作</summary>
    public const byte AdministrativeAction = 0x98;

    /// <summary>载荷格式无效</summary>
    public const byte PayloadFormatInvalid = 0x99;

    /// <summary>不支持保留消息</summary>
    public const byte RetainNotSupported = 0x9A;

    /// <summary>不支持的 QoS</summary>
    public const byte QoSNotSupported = 0x9B;

    /// <summary>使用另一个服务器</summary>
    public const byte UseAnotherServer = 0x9C;

    /// <summary>服务器已移动</summary>
    public const byte ServerMoved = 0x9D;

    /// <summary>不支持共享订阅</summary>
    public const byte SharedSubscriptionsNotSupported = 0x9E;

    /// <summary>连接速率超限</summary>
    public const byte ConnectionRateExceeded = 0x9F;

    /// <summary>最大连接时间</summary>
    public const byte MaximumConnectTime = 0xA0;

    /// <summary>不支持订阅标识符</summary>
    public const byte SubscriptionIdentifiersNotSupported = 0xA1;

    /// <summary>不支持通配符订阅</summary>
    public const byte WildcardSubscriptionsNotSupported = 0xA2;

    /// <summary>
    /// 判断原因码是否表示成功。
    /// </summary>
    /// <param name="reasonCode">原因码</param>
    /// <returns>如果是成功码则返回 true</returns>
    public static bool IsSuccess(byte reasonCode) => reasonCode < 0x80;

    /// <summary>
    /// 判断原因码是否表示错误。
    /// </summary>
    /// <param name="reasonCode">原因码</param>
    /// <returns>如果是错误码则返回 true</returns>
    public static bool IsError(byte reasonCode) => reasonCode >= 0x80;

    /// <summary>
    /// 获取原因码的描述字符串。
    /// </summary>
    /// <param name="reasonCode">原因码</param>
    /// <returns>描述字符串</returns>
    public static string GetDescription(byte reasonCode)
    {
        return reasonCode switch
        {
            Success => "成功",
            GrantedQoS1 => "授予 QoS 1",
            GrantedQoS2 => "授予 QoS 2",
            DisconnectWithWillMessage => "带遗嘱消息断开",
            NoMatchingSubscribers => "无匹配的订阅者",
            NoSubscriptionExisted => "无订阅存在",
            ContinueAuthentication => "继续认证",
            ReAuthenticate => "重新认证",
            UnspecifiedError => "未指定错误",
            MalformedPacket => "畸形报文",
            ProtocolError => "协议错误",
            ImplementationSpecificError => "实现特定错误",
            UnsupportedProtocolVersion => "不支持的协议版本",
            ClientIdentifierNotValid => "客户端标识符无效",
            BadUserNameOrPassword => "用户名或密码错误",
            NotAuthorized => "未授权",
            ServerUnavailable => "服务器不可用",
            ServerBusy => "服务器繁忙",
            Banned => "禁止访问",
            ServerShuttingDown => "服务器关闭中",
            BadAuthenticationMethod => "错误的认证方法",
            KeepAliveTimeout => "保活超时",
            SessionTakenOver => "会话被接管",
            TopicFilterInvalid => "主题过滤器无效",
            TopicNameInvalid => "主题名无效",
            PacketIdentifierInUse => "报文标识符已被使用",
            PacketIdentifierNotFound => "报文标识符未找到",
            ReceiveMaximumExceeded => "接收最大值超限",
            TopicAliasInvalid => "主题别名无效",
            PacketTooLarge => "报文过大",
            MessageRateTooHigh => "消息速率过高",
            QuotaExceeded => "超出配额",
            AdministrativeAction => "管理操作",
            PayloadFormatInvalid => "载荷格式无效",
            RetainNotSupported => "不支持保留消息",
            QoSNotSupported => "不支持的 QoS",
            UseAnotherServer => "使用另一个服务器",
            ServerMoved => "服务器已移动",
            SharedSubscriptionsNotSupported => "不支持共享订阅",
            ConnectionRateExceeded => "连接速率超限",
            MaximumConnectTime => "最大连接时间",
            SubscriptionIdentifiersNotSupported => "不支持订阅标识符",
            WildcardSubscriptionsNotSupported => "不支持通配符订阅",
            _ => $"未知原因码 (0x{reasonCode:X2})"
        };
    }
}
