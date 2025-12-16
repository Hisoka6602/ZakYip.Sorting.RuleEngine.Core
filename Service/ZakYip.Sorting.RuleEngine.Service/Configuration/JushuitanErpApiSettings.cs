namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 聚水潭ERP API配置
/// Jushuituan ERP API settings
/// </summary>
public class JushuitanErpApiSettings
{
    /// <summary>
    /// API基础URL
    /// API base URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 合作伙伴密钥
    /// Partner key
    /// </summary>
    public string PartnerKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 合作伙伴密钥
    /// Partner secret
    /// </summary>
    public string PartnerSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// 访问令牌
    /// Access token
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
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
    
    /// <summary>
    /// 禁用SSL证书验证（仅用于开发/测试环境，生产环境必须为false）
    /// Disable SSL certificate validation (for development/testing only, MUST be false in production)
    /// </summary>
    public bool DisableSslValidation { get; set; } = false;
}
