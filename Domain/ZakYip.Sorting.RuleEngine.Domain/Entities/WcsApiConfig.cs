using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;
/// <summary>
/// WCS API配置实体（单例模式）
/// WCS API configuration entity (Singleton pattern)
/// </summary>
public record class WcsApiConfig
{
    /// <summary>
    /// 单例配置ID（固定为1）
    /// Singleton configuration ID (Fixed as 1)
    /// </summary>
    public const long SingletonId = 1L;
    
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    public long ConfigId { get; init; } = SingletonId;
    /// API名称
    public required string ApiName { get; init; }
    /// API基础URL
    public required string BaseUrl { get; init; }
    /// 超时时间（秒）
    public required int TimeoutSeconds { get; init; }
    /// API密钥（可选）
    public string? ApiKey { get; init; }
    /// 自定义请求头（JSON格式）
    public string? CustomHeaders { get; init; }
    /// 请求方法（GET、POST等）
    public required string HttpMethod { get; init; }
    /// 请求体模板（可包含占位符）
    public string? RequestBodyTemplate { get; init; }
    /// 是否启用
    public required bool IsEnabled { get; init; }
    /// 优先级（数值越小优先级越高）
    public required int Priority { get; init; }
    /// 备注说明
    public string? Description { get; init; }
    /// 创建时间
    public DateTime CreatedAt { get; init; } = SystemClockProvider.LocalNow;
    /// 最后更新时间
    public DateTime UpdatedAt { get; init; } = SystemClockProvider.LocalNow;
}
