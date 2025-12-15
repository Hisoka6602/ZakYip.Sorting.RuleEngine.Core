using ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;

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
    /// 可选值: WcsApiClient, WdtWmsApiClient, JushuitanErpApiClient, PostCollectionApiClient, PostProcessingCenterApiClient
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
    /// 邮政处理中心API配置
    /// Postal Processing Center API configuration
    /// </summary>
    public PostProcessingCenterApiSettings PostProcessingCenterApi { get; set; } = new();

    /// <summary>
    /// 邮政分揽投机构API配置
    /// Postal Collection Institution API configuration
    /// </summary>
    public PostCollectionApiSettings PostCollectionApi { get; set; } = new();

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
    /// 日志文件清理配置 / Log file cleanup configuration
    /// 使用基础设施层的 LogFileCleanupSettings 消除影分身
    /// Using Infrastructure layer's LogFileCleanupSettings to eliminate shadow clone
    /// </summary>
    public LogFileCleanupSettings? LogFileCleanup { get; set; }
    
    /// <summary>
    /// DWS数据接收超时配置 / DWS data reception timeout settings
    /// </summary>
    public DwsTimeoutSettings DwsTimeout { get; set; } = new();
}
