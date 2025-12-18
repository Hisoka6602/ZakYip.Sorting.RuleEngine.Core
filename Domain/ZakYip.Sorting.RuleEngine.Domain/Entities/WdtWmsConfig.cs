namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 旺店通WMS API配置实体（单例模式）
/// WDT WMS API configuration entity (Singleton pattern)
/// </summary>
/// <remarks>
/// 根据项目要求，所有第三方API配置必须存储在LiteDB中，支持热更新
/// Per project requirements, all third-party API configurations must be stored in LiteDB with hot reload support
/// </remarks>
public record class WdtWmsConfig
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
    /// Sid (店铺ID)
    /// Shop ID
    /// </summary>
    public required string Sid { get; init; }
    
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
    /// API方法名
    /// API method name
    /// </summary>
    public string Method { get; init; } = "wms.logistics.Consign.weigh";
    
    /// <summary>
    /// 超时时间（毫秒）
    /// Timeout (milliseconds)
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;
    
    /// <summary>
    /// 是否必须包含包装条码
    /// Must include box barcode
    /// </summary>
    public bool MustIncludeBoxBarcode { get; init; } = false;
    
    /// <summary>
    /// 默认重量
    /// Default weight
    /// </summary>
    public double DefaultWeight { get; init; } = 0.0;
    
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
