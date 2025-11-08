namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 邮政处理中心API配置
/// Postal Processing Center API settings
/// </summary>
public class PostProcessingCenterApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = false;
}
