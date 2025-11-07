namespace ZakYip.Sorting.RuleEngine.Domain.Constants;

/// <summary>
/// 性能相关常量定义
/// Performance-related constants
/// </summary>
public static class PerformanceConstants
{
    /// <summary>
    /// 格口每小时最大处理能力（包裹数）
    /// Maximum capacity per chute per hour (parcels)
    /// </summary>
    public const int MaxChuteCapacityPerHour = 600;

    /// <summary>
    /// 重试策略 - 最大重试次数
    /// Retry policy - Maximum retry attempts
    /// </summary>
    public const int MaxRetryAttempts = 3;

    /// <summary>
    /// 重试策略 - 初始延迟（毫秒）
    /// Retry policy - Initial delay in milliseconds
    /// </summary>
    public const int RetryInitialDelayMs = 100;

    /// <summary>
    /// 数据查询 - 最大查询前后记录数
    /// Data query - Maximum records before/after
    /// </summary>
    public const int MaxQuerySurroundingRecords = 100;

    /// <summary>
    /// 百分比计算 - 100%
    /// Percentage calculation - 100%
    /// </summary>
    public const int MaxPercentage = 100;

    /// <summary>
    /// 缓存过期时间 - 绝对过期（秒）
    /// Cache expiration - Absolute expiration in seconds
    /// </summary>
    public const int CacheAbsoluteExpirationSeconds = 3600;

    /// <summary>
    /// 缓存过期时间 - 滑动过期（秒）
    /// Cache expiration - Sliding expiration in seconds
    /// </summary>
    public const int CacheSlidingExpirationSeconds = 600;
}
