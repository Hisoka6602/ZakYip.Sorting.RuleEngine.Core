namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// API请求日志实体
/// 记录所有对本站的API请求日志
/// </summary>
public class ApiRequestLog
{
    /// <summary>
    /// 日志ID（主键，自增）
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTime RequestTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 请求IP地址
    /// </summary>
    public string RequestIp { get; set; } = string.Empty;

    /// <summary>
    /// 请求方法（GET/POST/PUT/DELETE等）
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// 请求路由/路径
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// 请求查询字符串
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// 请求头（JSON格式）
    /// </summary>
    public string? RequestHeaders { get; set; }

    /// <summary>
    /// 请求体内容
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTime? ResponseTime { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// 响应头（JSON格式）
    /// </summary>
    public string? ResponseHeaders { get; set; }

    /// <summary>
    /// 响应内容
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 用户标识（如有）
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 请求是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误消息（如有）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
