using ZakYip.Sorting.RuleEngine.Domain.Services;

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
    /// 请求时间
    public DateTime RequestTime { get; set; } = SystemClockProvider.LocalNow;
    /// 请求IP地址
    public string RequestIp { get; set; } = string.Empty;
    /// 请求方法（GET/POST/PUT/DELETE等）
    public string RequestMethod { get; set; } = string.Empty;
    /// 请求路由/路径
    public string RequestPath { get; set; } = string.Empty;
    /// 请求查询字符串
    public string? QueryString { get; set; }
    /// 请求头（JSON格式）
    public string? RequestHeaders { get; set; }
    /// 请求体内容
    public string? RequestBody { get; set; }
    /// 响应时间
    public DateTime? ResponseTime { get; set; }
    /// 响应状态码
    public int? ResponseStatusCode { get; set; }
    /// 响应头（JSON格式）
    public string? ResponseHeaders { get; set; }
    /// 响应内容
    public string? ResponseBody { get; set; }
    /// 耗时（毫秒）
    public long DurationMs { get; set; }
    /// 用户标识（如有）
    public string? UserId { get; set; }
    /// 请求是否成功
    public bool IsSuccess { get; set; }
    /// 错误消息（如有）
    public string? ErrorMessage { get; set; }
}
