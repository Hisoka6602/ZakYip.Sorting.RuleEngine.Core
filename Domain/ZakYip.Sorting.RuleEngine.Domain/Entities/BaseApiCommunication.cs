using ZakYip.Sorting.RuleEngine.Domain.Services;
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
    /// 请求地址
    /// Request URL
    public string RequestUrl { get; set; } = string.Empty;
    /// 请求内容
    /// Request body
    public string? RequestBody { get; set; }
    /// 请求头
    /// Request headers
    public string? RequestHeaders { get; set; }
    /// 请求时间
    /// Request time
    public DateTime RequestTime { get; set; } = SystemClockProvider.LocalNow;
    /// 耗时（毫秒）
    /// Duration in milliseconds
    public long DurationMs { get; set; }
    /// 响应时间
    /// Response time
    public DateTime? ResponseTime { get; set; }
    /// 响应内容
    /// Response body
    public string? ResponseBody { get; set; }
    /// 响应状态码
    /// Response status code
    public int? ResponseStatusCode { get; set; }
    /// 响应头
    /// Response headers
    public string? ResponseHeaders { get; set; }
    /// 格式化的Curl内容
    /// Formatted CURL content
    public string? FormattedCurl { get; set; }
}
