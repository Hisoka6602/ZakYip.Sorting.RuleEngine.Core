namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// WCS API配置
/// WCS API settings
/// </summary>
public class ThirdPartyApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// 禁用SSL证书验证（仅用于开发/测试环境，生产环境必须为false）
    /// Disable SSL certificate validation (for development/testing only, MUST be false in production)
    /// </summary>
    public bool DisableSslValidation { get; set; } = false;
}
