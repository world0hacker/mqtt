namespace System.Net.MQTT.CoAP.Protocol;

/// <summary>
/// CoAP 选项编号。
/// </summary>
public static class CoapOptionNumber
{
    /// <summary>
    /// If-Match (1) - 条件请求，仅当 ETag 匹配时执行。
    /// </summary>
    public const int IfMatch = 1;

    /// <summary>
    /// Uri-Host (3) - 目标 URI 的主机部分。
    /// </summary>
    public const int UriHost = 3;

    /// <summary>
    /// ETag (4) - 实体标签，用于缓存验证。
    /// </summary>
    public const int ETag = 4;

    /// <summary>
    /// If-None-Match (5) - 条件请求，仅当资源不存在时执行。
    /// </summary>
    public const int IfNoneMatch = 5;

    /// <summary>
    /// Observe (6) - 观察选项，用于订阅资源更新。
    /// </summary>
    public const int Observe = 6;

    /// <summary>
    /// Uri-Port (7) - 目标 URI 的端口部分。
    /// </summary>
    public const int UriPort = 7;

    /// <summary>
    /// Location-Path (8) - 响应中的资源位置路径。
    /// </summary>
    public const int LocationPath = 8;

    /// <summary>
    /// Uri-Path (11) - 目标 URI 的路径段。
    /// </summary>
    public const int UriPath = 11;

    /// <summary>
    /// Content-Format (12) - 有效载荷的内容格式。
    /// </summary>
    public const int ContentFormat = 12;

    /// <summary>
    /// Max-Age (14) - 响应的最大缓存时间（秒）。
    /// </summary>
    public const int MaxAge = 14;

    /// <summary>
    /// Uri-Query (15) - 目标 URI 的查询参数。
    /// </summary>
    public const int UriQuery = 15;

    /// <summary>
    /// Accept (17) - 可接受的响应内容格式。
    /// </summary>
    public const int Accept = 17;

    /// <summary>
    /// Location-Query (20) - 响应中的资源位置查询。
    /// </summary>
    public const int LocationQuery = 20;

    /// <summary>
    /// Block2 (23) - 块传输选项（响应块）。
    /// </summary>
    public const int Block2 = 23;

    /// <summary>
    /// Block1 (27) - 块传输选项（请求块）。
    /// </summary>
    public const int Block1 = 27;

    /// <summary>
    /// Size2 (28) - 响应大小指示。
    /// </summary>
    public const int Size2 = 28;

    /// <summary>
    /// Proxy-Uri (35) - 代理 URI。
    /// </summary>
    public const int ProxyUri = 35;

    /// <summary>
    /// Proxy-Scheme (39) - 代理方案。
    /// </summary>
    public const int ProxyScheme = 39;

    /// <summary>
    /// Size1 (60) - 请求大小指示。
    /// </summary>
    public const int Size1 = 60;

    /// <summary>
    /// 获取选项是否为关键选项。
    /// 关键选项必须被理解，否则返回 4.02 Bad Option。
    /// </summary>
    /// <param name="optionNumber">选项编号</param>
    /// <returns>是否为关键选项</returns>
    public static bool IsCritical(int optionNumber)
    {
        return (optionNumber & 1) == 1;
    }

    /// <summary>
    /// 获取选项是否为不安全转发选项。
    /// 代理在不理解时不应转发此选项。
    /// </summary>
    /// <param name="optionNumber">选项编号</param>
    /// <returns>是否为不安全转发选项</returns>
    public static bool IsUnsafe(int optionNumber)
    {
        return (optionNumber & 2) == 2;
    }

    /// <summary>
    /// 获取选项是否为 NoCacheKey 选项。
    /// </summary>
    /// <param name="optionNumber">选项编号</param>
    /// <returns>是否为 NoCacheKey 选项</returns>
    public static bool IsNoCacheKey(int optionNumber)
    {
        return (optionNumber & 0x1E) == 0x1C;
    }
}

/// <summary>
/// CoAP 内容格式。
/// </summary>
public static class CoapContentFormat
{
    /// <summary>
    /// text/plain; charset=utf-8
    /// </summary>
    public const int TextPlain = 0;

    /// <summary>
    /// application/link-format
    /// </summary>
    public const int LinkFormat = 40;

    /// <summary>
    /// application/xml
    /// </summary>
    public const int ApplicationXml = 41;

    /// <summary>
    /// application/octet-stream
    /// </summary>
    public const int OctetStream = 42;

    /// <summary>
    /// application/exi
    /// </summary>
    public const int Exi = 47;

    /// <summary>
    /// application/json
    /// </summary>
    public const int Json = 50;

    /// <summary>
    /// application/cbor
    /// </summary>
    public const int Cbor = 60;
}
