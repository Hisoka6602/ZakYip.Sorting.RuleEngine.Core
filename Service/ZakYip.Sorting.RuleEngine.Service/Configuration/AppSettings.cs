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
    /// WCS API配置
    /// WCS API configuration
    /// </summary>
    public ThirdPartyApiSettings WcsApi { get; set; } = new();

    /// <summary>
    /// 激活的WCS API适配器类型
    /// Active wcs API adapter type
    /// 可选值: WcsApiClient, WdtWmsApiClient, JushuitanErpApiClient, PostCollectionApiAdapter, PostProcessingCenterApiAdapter
    /// </summary>
    public string ActiveApiAdapter { get; set; } = "WcsApiClient";

    /// <summary>
    /// 旺店通WMS API配置
    /// WDT WMS API configuration
    /// </summary>
    public WdtWmsApiSettings WdtWmsApi { get; set; } = new();

    /// <summary>
    /// 聚水潭ERP API配置
    /// Jushuituan ERP API configuration
    /// </summary>
    public JushuitanErpApiSettings JushuitanErpApi { get; set; } = new();

    /// <summary>
    /// MiniAPI配置
    /// MiniAPI configuration
    /// </summary>
    public MiniApiSettings MiniApi { get; set; } = new();
    
    /// <summary>
    /// 缓存配置
    /// Cache configuration
    /// </summary>
    public CacheSettings Cache { get; set; } = new();
    
    /// <summary>
    /// 日志文件清理配置
    /// Log file cleanup configuration
    /// </summary>
    public LogFileCleanupSettings? LogFileCleanup { get; set; }
}
