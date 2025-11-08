namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// ApiClient测试响应
/// ApiClient Test Response
/// </summary>
public class ApiClientTestResponse
{
    /// <summary>
    /// 是否成功
    /// Success flag
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应代码
    /// Response code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 响应消息
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据
    /// Response data
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public string? ParcelId { get; set; }

    /// <summary>
    /// 请求URL
    /// Request URL
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// 请求体
    /// Request body
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 响应体
    /// Response body
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// 错误消息
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 请求时间
    /// Request time
    /// </summary>
    public DateTime RequestTime { get; set; }

    /// <summary>
    /// 响应时间
    /// Response time
    /// </summary>
    public DateTime? ResponseTime { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 响应状态码
    /// Response status code
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// 格式化的CURL命令
    /// Formatted CURL command
    /// </summary>
    public string? FormattedCurl { get; set; }
}
