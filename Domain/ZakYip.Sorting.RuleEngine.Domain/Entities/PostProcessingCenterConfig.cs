namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 邮政处理中心API配置实体（单例模式）
/// Postal Processing Center API configuration entity (Singleton pattern)
/// </summary>
/// <remarks>
/// 根据项目要求，所有第三方API配置必须存储在LiteDB中，支持热更新
/// Per project requirements, all third-party API configurations must be stored in LiteDB with hot reload support
/// 配置字段与 ApiParameters 结构保持一致
/// Configuration fields are consistent with ApiParameters structure
/// </remarks>
public record class PostProcessingCenterConfig
{
    /// <summary>
    /// 单例配置ID（固定为"PostProcessingCenterConfigId"）
    /// Singleton configuration ID (Fixed as "PostProcessingCenterConfigId")
    /// </summary>
    public const string SingletonId = "PostProcessingCenterConfigId";
    
    /// <summary>
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    /// </summary>
    public string ConfigId { get; init; } = SingletonId;
    
    /// <summary>
    /// API接口URL
    /// API endpoint URL
    /// </summary>
    /// <example>http://10.4.188.85/pcs-tc-nc-job/WyService/services/CommWY?wsdl</example>
    public required string Url { get; init; }
    
    /// <summary>
    /// 超时时间 (毫秒)
    /// Timeout (milliseconds)
    /// </summary>
    /// <example>1000</example>
    public int Timeout { get; init; } = 1000;
    
    /// <summary>
    /// 车间代码
    /// Workshop code
    /// </summary>
    /// <example>WS43400001</example>
    public required string WorkshopCode { get; init; }
    
    /// <summary>
    /// 设备ID
    /// Device ID
    /// </summary>
    /// <example>43400002</example>
    public required string DeviceId { get; init; }
    
    /// <summary>
    /// 员工号
    /// Employee number
    /// </summary>
    /// <example>03178298</example>
    public required string EmployeeNumber { get; init; }
    
    /// <summary>
    /// 本地服务Url
    /// Local service URL
    /// </summary>
    public string LocalServiceUrl { get; init; } = string.Empty;
    
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
