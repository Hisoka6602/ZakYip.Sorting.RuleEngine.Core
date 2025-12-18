namespace ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

/// <summary>
/// 旺店通ERP旗舰版 API配置请求
/// WDT ERP Flagship API Configuration Request
/// </summary>
public record WdtErpFlagshipConfigRequest
{
    /// <summary>
    /// 配置名称
    /// Configuration name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// API接口地址
    /// </summary>
    public required string Url { get; set; }
    
    /// <summary>
    /// Key (应用标识)
    /// </summary>
    public required string Key { get; set; }
    
    /// <summary>
    /// Appsecret (应用密钥)
    /// </summary>
    public required string Appsecret { get; set; }
    
    /// <summary>
    /// Sid (店铺ID)
    /// </summary>
    public required string Sid { get; set; }
    
    /// <summary>
    /// Method (API方法名称)
    /// 支持的方法:
    /// - wms.stockout.Sales.weighingExt: 扩展称重
    /// - wms.stockout.Sales.onceWeighing: 一次称重(按打包员ID)
    /// - wms.stockout.Sales.onceWeighingByNo: 一次称重(按打包员编号)
    /// </summary>
    public string Method { get; set; } = "wms.stockout.Sales.weighingExt";
    
    /// <summary>
    /// V (API版本号)
    /// </summary>
    public required string V { get; set; }
    
    /// <summary>
    /// Salt (加密盐值)
    /// </summary>
    public required string Salt { get; set; }
    
    /// <summary>
    /// 打包员ID (用于weighingExt和onceWeighing方法)
    /// </summary>
    public int PackagerId { get; set; }
    
    /// <summary>
    /// 打包员编号 (用于onceWeighingByNo方法)
    /// </summary>
    public string PackagerNo { get; set; } = string.Empty;
    
    /// <summary>
    /// 打包台名称 (用于onceWeighing和onceWeighingByNo方法)
    /// </summary>
    public string OperateTableName { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否强制称重
    /// </summary>
    public bool Force { get; set; }
    
    /// <summary>
    /// 超时时间（毫秒）
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;
    
    /// <summary>
    /// 是否启用
    /// Whether enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 备注说明
    /// Description
    /// </summary>
    public string? Description { get; set; }
}
