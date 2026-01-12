using System.Text;

namespace System.Net.MQTT.CoAP.Protocol;

/// <summary>
/// CoAP 消息。
///
/// CoAP 消息格式:
/// | Ver(2) | Type(2) | TKL(4) | Code(8) | Message ID(16) |
/// | Token (0-8 bytes) |
/// | Options... |
/// | 0xFF | Payload... |
/// </summary>
public sealed class CoapMessage
{
    /// <summary>
    /// CoAP 协议版本，固定为 1。
    /// </summary>
    public const int Version = 1;

    /// <summary>
    /// 有效载荷标记字节。
    /// </summary>
    public const byte PayloadMarker = 0xFF;

    /// <summary>
    /// 获取或设置消息类型。
    /// </summary>
    public CoapMessageType Type { get; set; }

    /// <summary>
    /// 获取或设置代码（请求方法或响应代码）。
    /// </summary>
    public CoapCode Code { get; set; }

    /// <summary>
    /// 获取或设置消息 ID。
    /// 用于匹配请求和响应、检测重复消息。
    /// </summary>
    public ushort MessageId { get; set; }

    /// <summary>
    /// 获取或设置令牌。
    /// 用于将响应与请求关联（独立于 Message ID）。
    /// </summary>
    public byte[] Token { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 获取或设置选项列表。
    /// </summary>
    public List<CoapOption> Options { get; set; } = new();

    /// <summary>
    /// 获取或设置有效载荷。
    /// </summary>
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 获取令牌长度 (0-8)。
    /// </summary>
    public int TokenLength => Token.Length;

    /// <summary>
    /// 获取有效载荷作为 UTF-8 字符串。
    /// </summary>
    public string PayloadString => Payload.Length > 0 ? Encoding.UTF8.GetString(Payload) : string.Empty;

    /// <summary>
    /// 获取是否为请求消息。
    /// </summary>
    public bool IsRequest => Code.IsRequest;

    /// <summary>
    /// 获取是否为响应消息。
    /// </summary>
    public bool IsResponse => Code.IsSuccess || Code.IsClientError || Code.IsServerError;

    /// <summary>
    /// 获取是否为空消息 (0.00)。
    /// </summary>
    public bool IsEmpty => Code.IsEmpty;

    /// <summary>
    /// 获取是否为可确认消息。
    /// </summary>
    public bool IsConfirmable => Type == CoapMessageType.Confirmable;

    /// <summary>
    /// 获取是否需要确认。
    /// </summary>
    public bool NeedsConfirmation => Type == CoapMessageType.Confirmable;

    #region 选项辅助方法

    /// <summary>
    /// 获取第一个匹配的选项值。
    /// </summary>
    /// <param name="optionNumber">选项编号</param>
    /// <returns>选项，如果不存在返回 null</returns>
    public CoapOption? GetOption(int optionNumber)
    {
        foreach (var option in Options)
        {
            if (option.Number == optionNumber)
                return option;
        }
        return null;
    }

    /// <summary>
    /// 获取所有匹配的选项。
    /// </summary>
    /// <param name="optionNumber">选项编号</param>
    /// <returns>选项列表</returns>
    public IEnumerable<CoapOption> GetOptions(int optionNumber)
    {
        foreach (var option in Options)
        {
            if (option.Number == optionNumber)
                yield return option;
        }
    }

    /// <summary>
    /// 添加选项。
    /// </summary>
    /// <param name="option">要添加的选项</param>
    public void AddOption(CoapOption option)
    {
        Options.Add(option);
    }

    /// <summary>
    /// 添加选项。
    /// </summary>
    /// <param name="number">选项编号</param>
    /// <param name="value">选项值</param>
    public void AddOption(int number, string value)
    {
        Options.Add(new CoapOption(number, value));
    }

    /// <summary>
    /// 添加选项。
    /// </summary>
    /// <param name="number">选项编号</param>
    /// <param name="value">选项值</param>
    public void AddOption(int number, uint value)
    {
        Options.Add(new CoapOption(number, value));
    }

    /// <summary>
    /// 获取 URI 路径（所有 Uri-Path 选项组合）。
    /// </summary>
    /// <returns>URI 路径</returns>
    public string GetUriPath()
    {
        var paths = new List<string>();
        foreach (var option in GetOptions(CoapOptionNumber.UriPath))
        {
            paths.Add(option.GetStringValue());
        }
        return string.Join("/", paths);
    }

    /// <summary>
    /// 设置 URI 路径。
    /// </summary>
    /// <param name="path">URI 路径</param>
    public void SetUriPath(string path)
    {
        // 移除现有 Uri-Path 选项
        Options.RemoveAll(o => o.Number == CoapOptionNumber.UriPath);

        // 添加新的 Uri-Path 选项
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            AddOption(CoapOptionNumber.UriPath, segment);
        }
    }

    /// <summary>
    /// 获取查询字符串（所有 Uri-Query 选项组合）。
    /// </summary>
    /// <returns>查询字符串</returns>
    public string GetUriQuery()
    {
        var queries = new List<string>();
        foreach (var option in GetOptions(CoapOptionNumber.UriQuery))
        {
            queries.Add(option.GetStringValue());
        }
        return string.Join("&", queries);
    }

    /// <summary>
    /// 获取内容格式。
    /// </summary>
    /// <returns>内容格式，如果未设置返回 null</returns>
    public int? GetContentFormat()
    {
        var option = GetOption(CoapOptionNumber.ContentFormat);
        return option?.GetUintValue() as int?;
    }

    /// <summary>
    /// 设置内容格式。
    /// </summary>
    /// <param name="contentFormat">内容格式</param>
    public void SetContentFormat(int contentFormat)
    {
        Options.RemoveAll(o => o.Number == CoapOptionNumber.ContentFormat);
        AddOption(CoapOptionNumber.ContentFormat, (uint)contentFormat);
    }

    /// <summary>
    /// 获取 Observe 选项值。
    /// </summary>
    /// <returns>Observe 值，如果未设置返回 null</returns>
    public uint? GetObserve()
    {
        var option = GetOption(CoapOptionNumber.Observe);
        return option?.GetUintValue();
    }

    /// <summary>
    /// 设置 Observe 选项。
    /// </summary>
    /// <param name="value">Observe 值 (0=注册, 1=注销)</param>
    public void SetObserve(uint value)
    {
        Options.RemoveAll(o => o.Number == CoapOptionNumber.Observe);
        AddOption(CoapOptionNumber.Observe, value);
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 创建请求消息。
    /// </summary>
    /// <param name="type">消息类型</param>
    /// <param name="code">请求代码</param>
    /// <param name="messageId">消息 ID</param>
    /// <param name="token">令牌</param>
    /// <returns>请求消息</returns>
    public static CoapMessage CreateRequest(
        CoapMessageType type,
        CoapCode code,
        ushort messageId,
        byte[]? token = null)
    {
        return new CoapMessage
        {
            Type = type,
            Code = code,
            MessageId = messageId,
            Token = token ?? Array.Empty<byte>()
        };
    }

    /// <summary>
    /// 创建响应消息。
    /// </summary>
    /// <param name="request">原始请求</param>
    /// <param name="code">响应代码</param>
    /// <returns>响应消息</returns>
    public static CoapMessage CreateResponse(CoapMessage request, CoapCode code)
    {
        return new CoapMessage
        {
            Type = request.IsConfirmable ? CoapMessageType.Acknowledgement : CoapMessageType.NonConfirmable,
            Code = code,
            MessageId = request.MessageId,
            Token = request.Token
        };
    }

    /// <summary>
    /// 创建 ACK 消息。
    /// </summary>
    /// <param name="messageId">要确认的消息 ID</param>
    /// <returns>ACK 消息</returns>
    public static CoapMessage CreateAck(ushort messageId)
    {
        return new CoapMessage
        {
            Type = CoapMessageType.Acknowledgement,
            Code = CoapCode.Empty,
            MessageId = messageId
        };
    }

    /// <summary>
    /// 创建 RST 消息。
    /// </summary>
    /// <param name="messageId">要重置的消息 ID</param>
    /// <returns>RST 消息</returns>
    public static CoapMessage CreateReset(ushort messageId)
    {
        return new CoapMessage
        {
            Type = CoapMessageType.Reset,
            Code = CoapCode.Empty,
            MessageId = messageId
        };
    }

    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"CoAP {Type} {Code} MID={MessageId} TKL={TokenLength} Options={Options.Count} Payload={Payload.Length}B";
    }
}
