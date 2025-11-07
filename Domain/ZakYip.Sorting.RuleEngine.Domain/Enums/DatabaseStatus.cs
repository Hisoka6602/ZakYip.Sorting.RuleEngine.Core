namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 数据库状态
/// </summary>
public enum DatabaseStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// 降级（使用SQLite）
    /// </summary>
    Degraded = 2,

    /// <summary>
    /// 熔断
    /// </summary>
    CircuitBroken = 3
}
