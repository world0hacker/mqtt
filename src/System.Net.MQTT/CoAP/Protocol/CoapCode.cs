using System.Runtime.CompilerServices;

namespace System.Net.MQTT.CoAP.Protocol;

/// <summary>
/// CoAP 代码类。
/// 包含请求方法和响应代码。
/// 格式: class.detail (3 bits + 5 bits)
/// </summary>
public readonly struct CoapCode : IEquatable<CoapCode>
{
    private readonly byte _value;

    /// <summary>
    /// 创建 CoAP 代码。
    /// </summary>
    /// <param name="value">原始字节值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoapCode(byte value)
    {
        _value = value;
    }

    /// <summary>
    /// 创建 CoAP 代码。
    /// </summary>
    /// <param name="codeClass">代码类 (0-7)</param>
    /// <param name="detail">详细代码 (0-31)</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CoapCode(int codeClass, int detail)
    {
        _value = (byte)((codeClass << 5) | (detail & 0x1F));
    }

    /// <summary>
    /// 获取原始字节值。
    /// </summary>
    public byte Value => _value;

    /// <summary>
    /// 获取代码类 (0-7)。
    /// </summary>
    public int CodeClass => (_value >> 5) & 0x07;

    /// <summary>
    /// 获取详细代码 (0-31)。
    /// </summary>
    public int Detail => _value & 0x1F;

    /// <summary>
    /// 获取是否为请求代码。
    /// </summary>
    public bool IsRequest => CodeClass == 0 && Detail != 0;

    /// <summary>
    /// 获取是否为成功响应代码 (2.xx)。
    /// </summary>
    public bool IsSuccess => CodeClass == 2;

    /// <summary>
    /// 获取是否为客户端错误响应代码 (4.xx)。
    /// </summary>
    public bool IsClientError => CodeClass == 4;

    /// <summary>
    /// 获取是否为服务器错误响应代码 (5.xx)。
    /// </summary>
    public bool IsServerError => CodeClass == 5;

    /// <summary>
    /// 获取是否为空代码 (0.00)。
    /// </summary>
    public bool IsEmpty => _value == 0;

    #region 请求方法 (0.xx)

    /// <summary>
    /// 空代码 (0.00)。
    /// </summary>
    public static readonly CoapCode Empty = new(0, 0);

    /// <summary>
    /// GET 请求 (0.01)。
    /// </summary>
    public static readonly CoapCode Get = new(0, 1);

    /// <summary>
    /// POST 请求 (0.02)。
    /// </summary>
    public static readonly CoapCode Post = new(0, 2);

    /// <summary>
    /// PUT 请求 (0.03)。
    /// </summary>
    public static readonly CoapCode Put = new(0, 3);

    /// <summary>
    /// DELETE 请求 (0.04)。
    /// </summary>
    public static readonly CoapCode Delete = new(0, 4);

    /// <summary>
    /// FETCH 请求 (0.05)。
    /// </summary>
    public static readonly CoapCode Fetch = new(0, 5);

    /// <summary>
    /// PATCH 请求 (0.06)。
    /// </summary>
    public static readonly CoapCode Patch = new(0, 6);

    /// <summary>
    /// iPATCH 请求 (0.07)。
    /// </summary>
    public static readonly CoapCode iPatch = new(0, 7);

    #endregion

    #region 成功响应 (2.xx)

    /// <summary>
    /// 已创建 (2.01)。
    /// </summary>
    public static readonly CoapCode Created = new(2, 1);

    /// <summary>
    /// 已删除 (2.02)。
    /// </summary>
    public static readonly CoapCode Deleted = new(2, 2);

    /// <summary>
    /// 有效 (2.03)。
    /// </summary>
    public static readonly CoapCode Valid = new(2, 3);

    /// <summary>
    /// 已更改 (2.04)。
    /// </summary>
    public static readonly CoapCode Changed = new(2, 4);

    /// <summary>
    /// 内容 (2.05)。
    /// </summary>
    public static readonly CoapCode Content = new(2, 5);

    /// <summary>
    /// 继续 (2.31) - 用于块传输。
    /// </summary>
    public static readonly CoapCode Continue = new(2, 31);

    #endregion

    #region 客户端错误 (4.xx)

    /// <summary>
    /// 错误请求 (4.00)。
    /// </summary>
    public static readonly CoapCode BadRequest = new(4, 0);

    /// <summary>
    /// 未授权 (4.01)。
    /// </summary>
    public static readonly CoapCode Unauthorized = new(4, 1);

    /// <summary>
    /// 错误选项 (4.02)。
    /// </summary>
    public static readonly CoapCode BadOption = new(4, 2);

    /// <summary>
    /// 禁止 (4.03)。
    /// </summary>
    public static readonly CoapCode Forbidden = new(4, 3);

    /// <summary>
    /// 未找到 (4.04)。
    /// </summary>
    public static readonly CoapCode NotFound = new(4, 4);

    /// <summary>
    /// 方法不允许 (4.05)。
    /// </summary>
    public static readonly CoapCode MethodNotAllowed = new(4, 5);

    /// <summary>
    /// 不可接受 (4.06)。
    /// </summary>
    public static readonly CoapCode NotAcceptable = new(4, 6);

    /// <summary>
    /// 请求实体不完整 (4.08)。
    /// </summary>
    public static readonly CoapCode RequestEntityIncomplete = new(4, 8);

    /// <summary>
    /// 冲突 (4.09)。
    /// </summary>
    public static readonly CoapCode Conflict = new(4, 9);

    /// <summary>
    /// 前置条件失败 (4.12)。
    /// </summary>
    public static readonly CoapCode PreconditionFailed = new(4, 12);

    /// <summary>
    /// 请求实体太大 (4.13)。
    /// </summary>
    public static readonly CoapCode RequestEntityTooLarge = new(4, 13);

    /// <summary>
    /// 不支持的内容格式 (4.15)。
    /// </summary>
    public static readonly CoapCode UnsupportedContentFormat = new(4, 15);

    /// <summary>
    /// 请求实体不可处理 (4.22)。
    /// </summary>
    public static readonly CoapCode UnprocessableEntity = new(4, 22);

    /// <summary>
    /// 请求过多 (4.29)。
    /// </summary>
    public static readonly CoapCode TooManyRequests = new(4, 29);

    #endregion

    #region 服务器错误 (5.xx)

    /// <summary>
    /// 内部服务器错误 (5.00)。
    /// </summary>
    public static readonly CoapCode InternalServerError = new(5, 0);

    /// <summary>
    /// 未实现 (5.01)。
    /// </summary>
    public static readonly CoapCode NotImplemented = new(5, 1);

    /// <summary>
    /// 错误网关 (5.02)。
    /// </summary>
    public static readonly CoapCode BadGateway = new(5, 2);

    /// <summary>
    /// 服务不可用 (5.03)。
    /// </summary>
    public static readonly CoapCode ServiceUnavailable = new(5, 3);

    /// <summary>
    /// 网关超时 (5.04)。
    /// </summary>
    public static readonly CoapCode GatewayTimeout = new(5, 4);

    /// <summary>
    /// 代理不支持 (5.05)。
    /// </summary>
    public static readonly CoapCode ProxyingNotSupported = new(5, 5);

    #endregion

    /// <summary>
    /// 隐式转换为字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(CoapCode code) => code._value;

    /// <summary>
    /// 隐式转换从字节。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CoapCode(byte value) => new(value);

    /// <inheritdoc/>
    public bool Equals(CoapCode other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CoapCode code && Equals(code);

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// 相等运算符。
    /// </summary>
    public static bool operator ==(CoapCode left, CoapCode right) => left.Equals(right);

    /// <summary>
    /// 不等运算符。
    /// </summary>
    public static bool operator !=(CoapCode left, CoapCode right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => $"{CodeClass}.{Detail:D2}";
}
