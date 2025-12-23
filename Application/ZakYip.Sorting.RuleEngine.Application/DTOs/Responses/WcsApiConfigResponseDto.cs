namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;

/// <summary>
/// WCS API配置响应
/// WCS API configuration response
/// </summary>
public record class WcsApiConfigResponseDto
{
    /// <summary>
    /// API接口URL
    /// API endpoint URL
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// API密钥（已脱敏）
    /// API Key (masked)
    /// </summary>
    public string? ApiKeyMasked { get; init; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout (milliseconds)
    /// </summary>
    public required int TimeoutMs { get; init; }
    
    /// <summary>
    /// 是否禁用SSL验证
    /// Disable SSL validation
    /// </summary>
    public required bool DisableSslValidation { get; init; }
    
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
