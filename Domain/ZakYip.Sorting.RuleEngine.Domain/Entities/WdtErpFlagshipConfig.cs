namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 旺店通ERP旗舰版API配置实体（单例模式）
/// WDT ERP Flagship API configuration entity (Singleton pattern)
/// </summary>
/// <remarks>
/// 根据项目要求，所有第三方API配置必须存储在LiteDB中，支持热更新
/// Per project requirements, all third-party API configurations must be stored in LiteDB with hot reload support
/// </remarks>
public record class WdtErpFlagshipConfig
{
    /// <summary>
    /// 单例配置ID（固定为"WdtErpFlagshipConfigId"）
    /// Singleton configuration ID (Fixed as "WdtErpFlagshipConfigId")
    /// </summary>
    public const string SingletonId = "WdtErpFlagshipConfigId";
    
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
    /// Key (应用标识)
    /// Application Key
    /// </summary>
    public required string Key { get; init; }
    
    /// <summary>
    /// Appsecret (应用密钥)
    /// Application Secret
    /// </summary>
    public required string Appsecret { get; init; }
    
    /// <summary>
    /// Sid (店铺ID)
    /// Shop ID
    /// </summary>
    public required string Sid { get; init; }
    
    /// <summary>
    /// API方法名
    /// API method name (wms.stockout.Sales.weighingExt, wms.stockout.Sales.onceWeighing, wms.stockout.Sales.onceWeighingByNo)
    /// </summary>
    public string Method { get; init; } = "wms.stockout.Sales.weighingExt";
    
    /// <summary>
    /// API版本号
    /// API version
    /// </summary>
    public required string V { get; init; }
    
    /// <summary>
    /// 加密盐值
    /// Encryption salt
    /// </summary>
    public required string Salt { get; init; }
    
    /// <summary>
    /// 打包员ID
    /// Packager ID
    /// </summary>
    public int PackagerId { get; init; }
    
    /// <summary>
    /// 打包员编号
    /// Packager number
    /// </summary>
    public string PackagerNo { get; init; } = string.Empty;
    
    /// <summary>
    /// 打包台名称
    /// Operate table name
    /// </summary>
    public string OperateTableName { get; init; } = string.Empty;
    
    /// <summary>
    /// 是否强制称重
    /// Force weighing
    /// </summary>
    public bool Force { get; init; } = false;
    
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
