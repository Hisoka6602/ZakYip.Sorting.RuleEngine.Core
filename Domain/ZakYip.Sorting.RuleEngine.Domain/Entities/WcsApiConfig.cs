namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// WCS API配置实体（单例模式）
/// WCS API configuration entity (Singleton pattern)
/// </summary>
/// <remarks>
/// 根据项目要求，所有第三方API配置必须存储在LiteDB中，支持热更新
/// Per project requirements, all third-party API configurations must be stored in LiteDB with hot reload support
/// </remarks>
public record class WcsApiConfig
{
    /// <summary>
    /// 单例配置ID（固定为"WcsApiConfigId"）
    /// Singleton configuration ID (Fixed as "WcsApiConfigId")
    /// </summary>
    public const string SingletonId = "WcsApiConfigId";
    
    /// <summary>
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    /// </summary>
    public string ConfigId { get; init; } = SingletonId;
    
    /// <summary>
    /// API接口URL
    /// API endpoint URL
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// API密钥（可选）
    /// API Key (optional)
    /// </summary>
    public string? ApiKey { get; init; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout (milliseconds)
    /// </summary>
    public int TimeoutMs { get; init; } = 30000;
    
    /// <summary>
    /// 是否禁用SSL验证（仅用于开发/测试环境）
    /// Disable SSL validation (for development/testing only)
    /// </summary>
    public bool DisableSslValidation { get; init; } = false;
    
    /// <summary>
    /// 是否启用
    /// Is enabled
    /// </summary>
    public required bool IsEnabled { get; init; }
    
    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// 创建时间
    /// Created time
    /// </summary>
    public required DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// 最后更新时间
    /// Last updated time
    /// </summary>
    public required DateTime UpdatedAt { get; init; }
}
