namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// MiniAPI配置
/// MiniAPI settings
/// </summary>
public class MiniApiSettings
{
    public string[] Urls { get; set; } = new[] { "http://localhost:5000" };
    public bool EnableSwagger { get; set; } = true;
}
