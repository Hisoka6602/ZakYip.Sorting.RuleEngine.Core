namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 应用程序配置
/// Application configuration settings
/// </summary>
public class AppSettings
{
    /// <summary>
    /// LiteDB配置
    /// LiteDB configuration
    /// </summary>
    public LiteDbSettings LiteDb { get; set; } = new();

    /// <summary>
    /// MySQL配置
    /// MySQL configuration
    /// </summary>
    public MySqlSettings MySql { get; set; } = new();

    /// <summary>
    /// SQLite配置（降级方案）
    /// SQLite configuration (fallback)
    /// </summary>
    public SqliteSettings Sqlite { get; set; } = new();

    /// <summary>
    /// 第三方API配置
    /// Third-party API configuration
    /// </summary>
    public ThirdPartyApiSettings ThirdPartyApi { get; set; } = new();

    /// <summary>
    /// MiniAPI配置
    /// MiniAPI configuration
    /// </summary>
    public MiniApiSettings MiniApi { get; set; } = new();
}

/// <summary>
/// LiteDB配置
/// LiteDB settings
/// </summary>
public class LiteDbSettings
{
    public string ConnectionString { get; set; } = "Filename=./data/config.db;Connection=shared";
}

/// <summary>
/// MySQL配置
/// MySQL settings
/// </summary>
public class MySqlSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// SQLite配置
/// SQLite settings
/// </summary>
public class SqliteSettings
{
    public string ConnectionString { get; set; } = "Data Source=./data/logs.db";
}

/// <summary>
/// 第三方API配置
/// Third-party API settings
/// </summary>
public class ThirdPartyApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string? ApiKey { get; set; }
}

/// <summary>
/// MiniAPI配置
/// MiniAPI settings
/// </summary>
public class MiniApiSettings
{
    public string[] Urls { get; set; } = new[] { "http://localhost:5000" };
    public bool EnableSwagger { get; set; } = true;
}
