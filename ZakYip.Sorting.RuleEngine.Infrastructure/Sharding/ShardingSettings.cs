namespace ZakYip.Sorting.RuleEngine.Infrastructure.Sharding;

/// <summary>
/// 数据库分片配置
/// Database sharding configuration
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
    /// 自动清理时间（Cron表达式）
    /// Auto cleanup schedule (Cron expression)
    /// </summary>
    public string CleanupSchedule { get; set; } = "0 0 2 * * ?"; // 每天凌晨2点

    /// <summary>
    /// 自动归档时间（Cron表达式）
    /// Auto archive schedule (Cron expression)
    /// </summary>
    public string ArchiveSchedule { get; set; } = "0 0 3 * * ?"; // 每天凌晨3点
}
