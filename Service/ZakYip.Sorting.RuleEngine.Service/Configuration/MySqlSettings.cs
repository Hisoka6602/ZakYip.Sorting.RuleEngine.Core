using ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;

namespace ZakYip.Sorting.RuleEngine.Service.Configuration;

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
    /// 熔断器配置 / Circuit breaker configuration
    /// 使用基础设施层的 DatabaseCircuitBreakerSettings 消除影分身
    /// Using Infrastructure layer's DatabaseCircuitBreakerSettings to eliminate shadow clone
    /// </summary>
    public DatabaseCircuitBreakerSettings CircuitBreaker { get; set; } = new();
    
    /// <summary>
    /// 连接池配置
    /// Connection pool configuration
    /// </summary>
    public ConnectionPoolSettings ConnectionPool { get; set; } = new();
}

/// <summary>
/// 数据库连接池配置
/// Database connection pool settings
/// </summary>
public class ConnectionPoolSettings
{
    /// <summary>
    /// 最小连接池大小（默认：5）
    /// Minimum pool size (default: 5)
    /// </summary>
    public int MinPoolSize { get; set; } = 5;
    
    /// <summary>
    /// 最大连接池大小（默认：100）
    /// Maximum pool size (default: 100)
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;
    
    /// <summary>
    /// 连接生命周期，单位秒（默认：300秒=5分钟）
    /// Connection lifetime in seconds (default: 300 seconds = 5 minutes)
    /// </summary>
    public int ConnectionLifetimeSeconds { get; set; } = 300;
    
    /// <summary>
    /// 连接空闲超时时间，单位秒（默认：180秒=3分钟）
    /// Connection idle timeout in seconds (default: 180 seconds = 3 minutes)
    /// </summary>
    public int ConnectionIdleTimeoutSeconds { get; set; } = 180;
    
    /// <summary>
    /// 启用连接池（默认：true）
    /// Enable connection pooling (default: true)
    /// </summary>
    public bool Pooling { get; set; } = true;
    
    /// <summary>
    /// 连接超时时间，单位秒（默认：30秒）
    /// Connection timeout in seconds (default: 30 seconds)
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}
