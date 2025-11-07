namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// WCS API配置实体
/// 用于存储多个WCS API的配置信息
/// </summary>
public record class WcsApiConfig
{
    /// <summary>
    /// 配置ID（主键）
    /// </summary>
    public required string ConfigId { get; init; }

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
