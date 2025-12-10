namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// WCS API配置实体（单例模式）
/// WCS API configuration entity (Singleton pattern)
/// </summary>
public record class WcsApiConfig
{
    /// <summary>
    /// 单例配置ID（固定为1，不对外暴露）
    /// Singleton configuration ID (Fixed as 1, not exposed externally)
    /// </summary>
    internal const long SINGLETON_ID = 1L;
    
    /// <summary>
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    /// </summary>
    public long ConfigId { get; init; } = SINGLETON_ID;

    /// <summary>
    /// API名称
    /// </summary>
    public required string ApiName { get; init; }

    /// <summary>
    /// API基础URL
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public required int TimeoutSeconds { get; init; }

    /// <summary>
    /// API密钥（可选）
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// 自定义请求头（JSON格式）
    /// </summary>
    public string? CustomHeaders { get; init; }

    /// <summary>
    /// 请求方法（GET、POST等）
    /// </summary>
    public required string HttpMethod { get; init; }

    /// <summary>
    /// 请求体模板（可包含占位符）
    /// </summary>
    public string? RequestBodyTemplate { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// 优先级（数值越小优先级越高）
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// 备注说明
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.Now;
}
