namespace ZakYip.Sorting.RuleEngine.Service.Configuration.Settings;

/// <summary>
/// SQLite配置
/// </summary>
public class SqliteSettings
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=./data/logs.db";
}
