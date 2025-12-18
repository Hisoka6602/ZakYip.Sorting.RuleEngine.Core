namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 聚水潭ERP API配置实体（单例模式）
/// Jushuituan ERP API configuration entity (Singleton pattern)
/// </summary>
/// <remarks>
/// 根据项目要求，所有第三方API配置必须存储在LiteDB中，支持热更新
/// Per project requirements, all third-party API configurations must be stored in LiteDB with hot reload support
/// </remarks>
public record class JushuitanErpConfig
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
    /// AppKey
    /// Application Key
    /// </summary>
    public required string AppKey { get; init; }
    
    /// <summary>
    /// AppSecret
    /// Application Secret
    /// </summary>
    public required string AppSecret { get; init; }
    
    /// <summary>
    /// AccessToken
    /// Access Token
    /// </summary>
    public required string AccessToken { get; init; }
    
    /// <summary>
    /// 版本
    /// Version
    /// </summary>
    public int Version { get; init; } = 2;
    
    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout (milliseconds)
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;
    
    /// <summary>
    /// 是否上传重量
    /// Whether to upload weight
    /// </summary>
    public bool IsUploadWeight { get; init; } = true;
    
    /// <summary>
    /// 称重类型
    /// Weighing type: 0=验货后称重, 1=验货后称重并发货, 2=无须验货称重, 3=无须验货称重并发货, 4=发货后称重, 5=自动判断称重并发货
    /// </summary>
    public int Type { get; init; } = 1;
    
    /// <summary>
    /// 是否为国际运单号
    /// Whether international waybill number
    /// </summary>
    public bool IsUnLid { get; init; } = false;
    
    /// <summary>
    /// 称重来源备注
    /// Weighing source remark
    /// </summary>
    public string Channel { get; init; } = string.Empty;
    
    /// <summary>
    /// 默认重量
    /// Default weight
    /// </summary>
    public double DefaultWeight { get; init; } = -1;
    
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
