namespace ZakYip.Sorting.RuleEngine.Service.Configuration.Settings;

/// <summary>
/// LiteDB配置
/// </summary>
public class LiteDbSettings
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Filename=./data/config.db;Connection=shared";
}
