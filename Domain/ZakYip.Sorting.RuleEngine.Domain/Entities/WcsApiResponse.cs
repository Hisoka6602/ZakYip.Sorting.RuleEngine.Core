using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// WCS API响应实体
/// WCS API response entity
/// 包含API请求和响应的完整信息
/// Contains complete information about API request and response
/// </summary>
public class WcsApiResponse
{
    /// <summary>
    /// 包裹Id / Parcel ID
    /// </summary>
    public required long ParcelId { get; init; }

    /// <summary>
    /// 请求状态，表示请求的处理结果状态（成功、失败等）
    /// Request status, indicating the result status of the request (success, failure, etc.)
    /// </summary>
    public required ApiRequestStatus RequestStatus { get; set; }

    /// <summary>
    /// 请求体内容，通常为 JSON 或其他格式的请求数据
    /// Request body content, usually JSON or other format request data
    /// </summary>
    public required string RequestBody { get; set; }

    /// <summary>
    /// 响应体内容，API 返回的结果数据
    /// Response body content, result data returned by API
    /// </summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// 请求发起时间，记录请求开始的时间点
    /// Request initiation time, recording when the request started
    /// </summary>
    public required DateTime RequestTime { get; set; }

    /// <summary>
    /// 响应接收时间，记录请求完成收到响应的时间点
    /// Response reception time, recording when the response was received
    /// </summary>
    public DateTime ResponseTime { get; set; }

    /// <summary>
    /// 请求耗时，单位为毫秒，从请求发起到响应完成的时间间隔
    /// Request duration in milliseconds, time interval from request initiation to response completion
    /// </summary>
    public int ElapsedMilliseconds { get; set; }

    /// <summary>
    /// 查询参数字符串，包含 URL 中的查询参数信息
    /// Query parameter string, containing query parameter information in the URL
    /// </summary>
    public string QueryParams { get; set; } = string.Empty;

    /// <summary>
    /// 请求头信息，包含所有请求头键值对的序列化字符串
    /// Request header information, serialized string containing all request header key-value pairs
    /// </summary>
    public string Headers { get; set; } = string.Empty;

    /// <summary>
    /// 请求的完整 URL 地址
    /// Complete URL address of the request
    /// </summary>
    public required string RequestUrl { get; init; }

    /// <summary>
    /// 异常信息，记录请求过程中发生的异常详情（若有）
    /// Exception information, recording exception details during the request process (if any)
    /// </summary>
    public string Exception { get; set; } = string.Empty;

    /// <summary>
    /// API 请求方法(RequestChuteAsync、UploadImageAsync、NotifyChuteLandingAsync等)
    /// API request method (RequestChuteAsync, UploadImageAsync, NotifyChuteLandingAsync, etc.)
    /// </summary>
    public required string Method { get; init; }

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
