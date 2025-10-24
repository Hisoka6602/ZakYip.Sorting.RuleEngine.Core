namespace ZakYip.Sorting.RuleEngine.Service.Configuration.Settings;

/// <summary>
/// MiniAPI配置
/// </summary>
public class MiniApiSettings
{
    /// <summary>
    /// 监听URL列表
    /// </summary>
    public string[] Urls { get; set; } = new[] { "http://localhost:5000" };
    
    /// <summary>
    /// 是否启用Swagger
    /// </summary>
    public bool EnableSwagger { get; set; } = true;
}
