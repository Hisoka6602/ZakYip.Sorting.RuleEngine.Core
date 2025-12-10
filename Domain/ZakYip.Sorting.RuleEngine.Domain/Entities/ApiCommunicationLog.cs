using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// API通信日志实体
/// </summary>
public class ApiCommunicationLog
{
    /// <summary>
    /// 日志ID（自增主键）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 通信类型（HTTP为主，记录API调用方式）
    /// </summary>
    public CommunicationType CommunicationType { get; set; } = CommunicationType.Http;

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
    /// 响应内容
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
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
