namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 旺店通WMS API配置
/// WDT WMS API settings
/// </summary>
public class WdtWmsApiSettings
{
    /// <summary>
    /// API基础URL
    /// API base URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API应用密钥
    /// API app key
    /// </summary>
    public string AppKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API应用密钥
    /// API app secret
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
    
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
