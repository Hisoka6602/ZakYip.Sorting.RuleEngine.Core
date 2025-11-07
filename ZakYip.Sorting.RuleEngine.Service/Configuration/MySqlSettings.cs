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
    /// 熔断器配置
    /// Circuit breaker configuration
    /// </summary>
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
}
