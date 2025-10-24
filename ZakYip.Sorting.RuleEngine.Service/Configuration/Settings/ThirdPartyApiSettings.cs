namespace ZakYip.Sorting.RuleEngine.Service.Configuration.Settings;

/// <summary>
/// 第三方API配置
/// </summary>
public class ThirdPartyApiSettings
{
    /// <summary>
    /// 基础URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// API密钥
    /// </summary>
    public string? ApiKey { get; set; }
}
