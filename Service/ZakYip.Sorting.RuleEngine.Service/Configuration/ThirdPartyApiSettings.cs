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
}
