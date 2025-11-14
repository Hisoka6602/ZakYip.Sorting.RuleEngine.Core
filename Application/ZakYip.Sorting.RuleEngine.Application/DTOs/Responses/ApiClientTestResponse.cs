namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// ApiClient测试响应
/// ApiClient Test Response
/// </summary>
public record ApiClientTestResponse
{
    /// <summary>
    /// 是否成功
    /// Success flag
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// 响应代码
    /// Response code
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// 响应消息
    /// Response message
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// 响应数据
    /// Response data
    /// </summary>
    public string? Data { get; init; }

    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public string? ParcelId { get; init; }

    /// <summary>
    /// 请求URL
    /// Request URL
    /// </summary>
    public string? RequestUrl { get; init; }

    /// <summary>
    /// 请求体
    /// Request body
    /// </summary>
    public string? RequestBody { get; init; }

    /// <summary>
    /// 响应体
    /// Response body
    /// </summary>
    public string? ResponseBody { get; init; }

    /// <summary>
    /// 错误消息
    /// Error message
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 请求时间
    /// Request time
    /// </summary>
    public required DateTime RequestTime { get; init; }

    /// <summary>
    /// 响应时间
    /// Response time
    /// </summary>
    public DateTime? ResponseTime { get; init; }

    /// <summary>
    /// 耗时（毫秒）
    /// Duration in milliseconds
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// 响应状态码
    /// Response status code
    /// </summary>
    public int? ResponseStatusCode { get; init; }

    /// <summary>
    /// 格式化的CURL命令
    /// Formatted CURL command
    /// </summary>
    public string? FormattedCurl { get; init; }
}
