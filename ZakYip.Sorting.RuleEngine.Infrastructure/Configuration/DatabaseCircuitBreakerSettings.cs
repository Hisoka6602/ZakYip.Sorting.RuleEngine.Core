namespace ZakYip.Sorting.RuleEngine.Infrastructure.Configuration;

/// <summary>
/// 数据库熔断器配置
/// </summary>
public class DatabaseCircuitBreakerSettings
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
