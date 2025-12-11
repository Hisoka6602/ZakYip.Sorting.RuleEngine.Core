namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// API通信基类 - 提取ApiCommunicationLog和WcsApiResponse的共享属性
/// Base API Communication class - Extracts shared properties from ApiCommunicationLog and WcsApiResponse
/// </summary>
/// <remarks>
/// 此抽象基类消除了 ApiCommunicationLog 和 WcsApiResponse 之间的属性重复，
/// 遵循DRY原则。
/// This abstract base class eliminates property duplication between ApiCommunicationLog 
/// and WcsApiResponse, following the DRY principle.
/// </remarks>
public abstract class BaseApiCommunication
{
    /// <summary>
    /// 包裹ID
    /// Parcel ID
    /// </summary>
    public string ParcelId { get; set; } = string.Empty;

    /// <summary>
    /// 请求地址
    /// Request URL
    /// </summary>
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>
    /// 请求内容
    /// Request body
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 请求头
    /// Request headers
    /// </summary>
    public string? RequestHeaders { get; set; }

    /// <summary>
    /// 请求时间
    /// Request time
    /// </summary>
    public DateTime RequestTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 耗时（毫秒）
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 响应时间
    /// Response time
    /// </summary>
    public DateTime? ResponseTime { get; set; }

    /// <summary>
    /// 响应内容
    /// Response body
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// 响应状态码
    /// Response status code
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// 响应头
    /// Response headers
    /// </summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>
    /// 格式化的Curl内容
    /// Formatted CURL content
    /// </summary>
    public string? FormattedCurl { get; set; }
}
