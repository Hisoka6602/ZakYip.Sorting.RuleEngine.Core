namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtErpFlagship;

/// <summary>
/// 旺店通ERP旗舰版 API参数配置类
/// WDT ERP Flagship API Parameters Configuration
/// </summary>
public class WdtErpFlagshipApiParameters
{
    /// <summary>
    /// API接口地址
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Key (应用标识)
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Appsecret (应用密钥)
    /// </summary>
    public string Appsecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Sid (店铺ID)
    /// </summary>
    public string Sid { get; set; } = string.Empty;
    
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
    public string V { get; set; } = string.Empty;
    
    /// <summary>
    /// Salt (加密盐值)
    /// </summary>
    public string Salt { get; set; } = string.Empty;
    
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
    public int TimeOut { get; set; } = 5000;
}
