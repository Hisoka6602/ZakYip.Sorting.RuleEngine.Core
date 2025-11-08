namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 旺店通ERP旗舰版API配置
/// WDT ERP Flagship API settings
/// </summary>
public class WdtErpFlagshipApiSettings
{
    /// <summary>
    /// API基础URL
    /// API base URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API Key (应用标识)
    /// API key
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// API应用密钥
    /// API app secret
    /// </summary>
    public string Appsecret { get; set; } = string.Empty;
    
    /// <summary>
    /// 店铺ID
    /// Shop ID
    /// </summary>
    public string Sid { get; set; } = string.Empty;
    
    /// <summary>
    /// API版本号
    /// API version
    /// </summary>
    public string V { get; set; } = "1.0";
    
    /// <summary>
    /// 加密盐值
    /// Salt for encryption
    /// </summary>
    public string Salt { get; set; } = string.Empty;
    
    /// <summary>
    /// API方法名称
    /// API method name
    /// </summary>
    public string Method { get; set; } = "wms.stockout.Sales.weighingExt";
    
    /// <summary>
    /// 打包员ID
    /// Packager ID
    /// </summary>
    public int PackagerId { get; set; }
    
    /// <summary>
    /// 打包员编号
    /// Packager number
    /// </summary>
    public string PackagerNo { get; set; } = string.Empty;
    
    /// <summary>
    /// 打包台名称
    /// Operate table name
    /// </summary>
    public string OperateTableName { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否强制称重
    /// Force weighing
    /// </summary>
    public bool Force { get; set; } = false;
    
    /// <summary>
    /// API请求超时时间（秒），默认30秒
    /// API timeout in seconds, default 30
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 是否启用
    /// Enable or disable the API
    /// </summary>
    public bool Enabled { get; set; } = false;
}
