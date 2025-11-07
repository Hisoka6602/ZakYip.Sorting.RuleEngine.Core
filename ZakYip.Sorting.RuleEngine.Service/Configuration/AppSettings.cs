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
    /// 激活的第三方API适配器类型
    /// Active third-party API adapter type
    /// 可选值: ThirdPartyApiClient, WdtWmsApiClient, JushuitanErpApiClient
    /// </summary>
    public string ActiveApiAdapter { get; set; } = "ThirdPartyApiClient";

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
    
    /// <summary>
    /// MySQL服务器版本，例如 "8.0.33"，如果为空则使用默认版本
    /// MySQL server version, e.g., "8.0.33", uses default if empty
    /// </summary>
    public string? ServerVersion { get; set; }
    
    /// <summary>
    /// 熔断器配置
    /// Circuit breaker configuration
    /// </summary>
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
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

/// <summary>
/// 熔断器配置
/// Circuit breaker settings
/// </summary>
public class CircuitBreakerSettings
{
    /// <summary>
    /// 失败率阈值（0.0-1.0），默认0.5（50%）
    /// Failure ratio threshold (0.0-1.0), default 0.5 (50%)
    /// </summary>
    public decimal FailureRatio { get; set; } = 0.5m;
    
    /// <summary>
    /// 最小吞吐量（在采样周期内的最小请求数），默认10
    /// Minimum throughput (minimum number of requests in sampling duration), default 10
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;
    
    /// <summary>
    /// 采样周期（秒），默认30秒
    /// Sampling duration in seconds, default 30
    /// </summary>
    public int SamplingDurationSeconds { get; set; } = 30;
    
    /// <summary>
    /// 熔断持续时间（秒），默认1200秒（20分钟）
    /// Break duration in seconds, default 1200 (20 minutes)
    /// </summary>
    public int BreakDurationSeconds { get; set; } = 1200;
}

/// <summary>
/// 缓存配置
/// Cache settings
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// 绝对过期时间（秒），默认3600秒（1小时）
    /// Absolute expiration time in seconds, default 3600 (1 hour)
    /// </summary>
    public int AbsoluteExpirationSeconds { get; set; } = 3600;
    
    /// <summary>
    /// 滑动过期时间（秒），默认600秒（10分钟）
    /// Sliding expiration time in seconds, default 600 (10 minutes)
    /// </summary>
    public int SlidingExpirationSeconds { get; set; } = 600;
}

/// <summary>
/// 日志文件清理配置
/// Log file cleanup settings
/// </summary>
public class LogFileCleanupSettings
{
    /// <summary>
    /// 是否启用日志文件清理，默认true
    /// Enable log file cleanup, default true
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 日志保留天数，默认7天
    /// Log retention days, default 7
    /// </summary>
    public int RetentionDays { get; set; } = 7;
    
    /// <summary>
    /// 日志文件目录，默认"./logs"
    /// Log file directory, default "./logs"
    /// </summary>
    public string LogDirectory { get; set; } = "./logs";
}

/// <summary>
/// 旺店通WMS API配置
/// WDT WMS API settings
/// </summary>
public class WdtWmsApiSettings
{
    /// <summary>
    /// API基础URL
    /// API base URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// API应用密钥
    /// API app key
    /// </summary>
    public string AppKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API应用密钥
    /// API app secret
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// API请求超时时间（秒），默认30秒
    /// API timeout in seconds, default 30
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 是否启用
    /// Enable or disable the API
    /// </summary>
    public bool Enabled { get; set; } = false;
}

/// <summary>
/// 聚水潭ERP API配置
/// Jushuituan ERP API settings
/// </summary>
public class JushuitanErpApiSettings
{
    /// <summary>
    /// API基础URL
    /// API base URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 合作伙伴密钥
    /// Partner key
    /// </summary>
    public string PartnerKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 合作伙伴密钥
    /// Partner secret
    /// </summary>
    public string PartnerSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// 访问令牌
    /// Access token
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// API请求超时时间（秒），默认30秒
    /// API timeout in seconds, default 30
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// 是否启用
    /// Enable or disable the API
    /// </summary>
    public bool Enabled { get; set; } = false;
}
