namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// WCS API响应实体
/// 字段与ApiCommunicationLog一致，以便持久化
/// </summary>
public class WcsApiResponse
{
    /// <summary>
    /// 响应是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据（JSON格式）
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// 包裹ID
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 请求地址
    /// </summary>
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>
    /// 请求内容
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 请求头
    /// </summary>
    public string? RequestHeaders { get; set; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTime RequestTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTime? ResponseTime { get; set; }

    /// <summary>
    /// 响应内容（同Data，保持兼容）
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// 响应头
    /// </summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>
    /// 格式化的Curl内容
    /// </summary>
    public string? FormattedCurl { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// OCR识别数据（如果有）
    /// </summary>
    public OcrData? OcrData { get; set; }
}
