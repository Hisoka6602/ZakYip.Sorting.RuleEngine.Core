using ZakYip.Sorting.RuleEngine.Service.Configuration.Settings;

namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

/// <summary>
/// 应用程序配置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// LiteDB配置
    /// </summary>
    public LiteDbSettings LiteDb { get; set; } = new();

    /// <summary>
    /// MySQL配置
    /// </summary>
    public MySqlSettings MySql { get; set; } = new();

    /// <summary>
    /// SQLite配置（降级方案）
    /// </summary>
    public SqliteSettings Sqlite { get; set; } = new();

    /// <summary>
    /// 第三方API配置
    /// </summary>
    public ThirdPartyApiSettings ThirdPartyApi { get; set; } = new();

    /// <summary>
    /// MiniAPI配置
    /// </summary>
    public MiniApiSettings MiniApi { get; set; } = new();
    
    /// <summary>
    /// 缓存配置
    /// </summary>
    public CacheSettings Cache { get; set; } = new();
}
