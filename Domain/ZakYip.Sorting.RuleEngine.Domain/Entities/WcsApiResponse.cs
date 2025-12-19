using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// WCS API响应实体
/// WCS API response entity
/// 包含API请求和响应的完整信息
/// Contains complete information about API request and response
/// </summary>
/// <remarks>
/// 所有BaseApiCommunication字段（ParcelId, RequestUrl, RequestBody, RequestHeaders, RequestTime,
/// DurationMs, ResponseTime, ResponseBody, ResponseStatusCode, ResponseHeaders, FormattedCurl）
/// 必须在所有API调用中赋值，无论成功或失败
/// All BaseApiCommunication fields must be populated in all API calls, regardless of success or failure
/// </remarks>
public class WcsApiResponse : BaseApiCommunication
{
    /// <summary>
    /// 请求状态，表示请求的处理结果状态（成功、失败等）
    /// Request status, indicating the result status of the request (success, failure, etc.)
    /// </summary>
    public ApiRequestStatus RequestStatus { get; set; } = ApiRequestStatus.Success;

    /// <summary>
    /// 错误消息（如果请求失败）
    /// Error message (if request failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Curl组装数据，内容可直接用于Curl访问
    /// Curl assembled data, content can be directly used for Curl access
    /// </summary>
    public string CurlData { get; set; } = string.Empty;

    /// <summary>
    /// 格式化的消息内容，便于日志记录和分析的文本形式
    /// Formatted message content, text form for logging and analysis
    /// </summary>
    public string FormattedMessage { get; set; } = string.Empty;

    /// <summary>
    /// OCR识别数据（如果有）
    /// OCR recognition data (if any)
    /// </summary>
    public OcrData? OcrData { get; set; }
}
