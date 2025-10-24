namespace ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;

/// <summary>
/// 数据库分片配置
/// </summary>
public class ShardingSettings
{
    /// <summary>
    /// 是否启用分片
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 分片策略（Monthly, Daily, Weekly）
    /// Sharding strategy
    /// </summary>
    public string Strategy { get; set; } = "Monthly";

    /// <summary>
    /// 数据保留天数（用于自动清理）
    /// Data retention days for automatic cleanup
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// 冷数据阈值天数（超过此天数的数据视为冷数据）
    /// Cold data threshold days
    /// </summary>
    public int ColdDataThresholdDays { get; set; } = 30;

    /// <summary>
    /// 自动清理时间（Cron表达式）- 已废弃，使用IdleMinutesBeforeCleanup替代
    /// Auto cleanup schedule (Cron expression) - Deprecated, use IdleMinutesBeforeCleanup instead
    /// </summary>
    [Obsolete("使用IdleMinutesBeforeCleanup替代定时清理策略")]
    public string CleanupSchedule { get; set; } = "0 0 2 * * ?"; // 每天凌晨2点

    /// <summary>
    /// 自动归档时间（Cron表达式）
    /// Auto archive schedule (Cron expression)
    /// </summary>
    public string ArchiveSchedule { get; set; } = "0 0 3 * * ?"; // 每天凌晨3点

    /// <summary>
    /// 空闲多少分钟后开始清理数据（默认30分钟）
    /// Minutes of idle time before starting data cleanup (default 30 minutes)
    /// </summary>
    public int IdleMinutesBeforeCleanup { get; set; } = 30;

    /// <summary>
    /// 检查空闲状态的间隔（秒）
    /// Interval to check idle status (seconds)
    /// </summary>
    public int IdleCheckIntervalSeconds { get; set; } = 60;
}
