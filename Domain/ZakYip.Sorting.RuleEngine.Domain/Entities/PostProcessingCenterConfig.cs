namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 邮政处理中心API配置实体（单例模式）
/// Postal Processing Center API configuration entity (Singleton pattern)
/// </summary>
/// <remarks>
/// 根据项目要求，所有第三方API配置必须存储在LiteDB中，支持热更新
/// Per project requirements, all third-party API configurations must be stored in LiteDB with hot reload support
/// </remarks>
public record class PostProcessingCenterConfig
{
    /// <summary>
    /// 单例配置ID（固定为1）
    /// Singleton configuration ID (Fixed as 1)
    /// </summary>
    public const long SingletonId = 1L;
    
    /// <summary>
    /// 配置ID（主键）- 内部使用
    /// Configuration ID (Primary Key) - Internal use only
    /// </summary>
    public long ConfigId { get; init; } = SingletonId;
    
    /// <summary>
    /// 配置名称
    /// Configuration name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// API接口URL
    /// API endpoint URL
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// 车间代码
    /// Workshop code
    /// </summary>
    public required string WorkshopCode { get; init; }
    
    /// <summary>
    /// 设备ID
    /// Device ID
    /// </summary>
    public required string DeviceId { get; init; }
    
    /// <summary>
    /// 公司名称
    /// Company name
    /// </summary>
    public required string CompanyName { get; init; }
    
    /// <summary>
    /// 设备条码
    /// Device barcode
    /// </summary>
    public required string DeviceBarcode { get; init; }
    
    /// <summary>
    /// 机构编号
    /// Organization number
    /// </summary>
    public required string OrganizationNumber { get; init; }
    
    /// <summary>
    /// 员工编号
    /// Employee number
    /// </summary>
    public required string EmployeeNumber { get; init; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout (milliseconds)
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;
    
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
