namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 邮政分揽投机构API配置
/// Postal Collection Institution API settings
/// </summary>
public class PostCollectionApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public bool Enabled { get; set; } = false;
}
