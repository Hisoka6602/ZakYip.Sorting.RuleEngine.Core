namespace ZakYip.Sorting.RuleEngine.Service.Configuration.Settings;

/// <summary>
/// MySQL配置
/// </summary>
public class MySqlSettings
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 熔断器配置
    /// </summary>
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
}
