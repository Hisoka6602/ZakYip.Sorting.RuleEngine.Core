using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// WCS API响应实体
/// WCS API response entity
/// 包含API请求和响应的完整信息
/// Contains complete information about API request and response
/// </summary>
public class WcsApiResponse : BaseApiCommunication
{
    /// <summary>
    /// 请求状态，表示请求的处理结果状态（成功、失败等）
    /// Request status, indicating the result status of the request (success, failure, etc.)
    /// </summary>
    public ApiRequestStatus RequestStatus { get; set; } = ApiRequestStatus.Success;

    /// <summary>
    /// 状态码（字符串格式，通常为HTTP状态码）
    /// Status code (string format, usually HTTP status code)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 请求是否成功
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（如果请求失败）
    /// Error message (if request failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 响应消息（通用消息字段）
    /// Response message (general message field)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 响应数据（用于API匹配等场景）
    /// Response data (for API matching scenarios, etc.)
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// 查询参数字符串，包含 URL 中的查询参数信息
    /// Query parameter string, containing query parameter information in the URL
    /// </summary>
    public string QueryParams { get; set; } = string.Empty;

    /// <summary>
    /// 异常信息，记录请求过程中发生的异常详情（若有）
    /// Exception information, recording exception details during the request process (if any)
    /// </summary>
    public string Exception { get; set; } = string.Empty;

    /// <summary>
    /// API 请求方法(RequestChuteAsync、UploadImageAsync、NotifyChuteLandingAsync等)
    /// API request method (RequestChuteAsync, UploadImageAsync, NotifyChuteLandingAsync, etc.)
    /// </summary>
    public string Method { get; init; } = string.Empty;

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
