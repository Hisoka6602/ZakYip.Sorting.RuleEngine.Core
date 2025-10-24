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
    
    /// <summary>
    /// 缓存配置
    /// Cache configuration
    /// </summary>
    public CacheSettings Cache { get; set; } = new();
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
    public double FailureRatio { get; set; } = 0.5;
    
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
