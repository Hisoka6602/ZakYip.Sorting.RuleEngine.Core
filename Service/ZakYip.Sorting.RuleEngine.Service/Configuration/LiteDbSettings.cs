namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// LiteDB配置
/// LiteDB settings
/// </summary>
public class LiteDbSettings
{
    public string ConnectionString { get; set; } = "Filename=./data/config.db;Connection=shared";
}
