namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// SQLite配置
/// SQLite settings
/// </summary>
public class SqliteSettings
{
    public string ConnectionString { get; set; } = "Data Source=./data/logs.db";
}
