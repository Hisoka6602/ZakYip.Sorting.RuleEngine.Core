namespace ZakYip.Sorting.RuleEngine.Domain.Enums;

/// <summary>
/// 告警类型
/// Alert type
/// </summary>
public enum AlertType
{
    /// <summary>
    /// 包裹处理量
    /// </summary>
    ParcelProcessing = 1,

    /// <summary>
    /// 格口使用率
    /// </summary>
    ChuteUsage = 2,

    /// <summary>
    /// 性能指标
    /// </summary>
    PerformanceMetric = 3,

    /// <summary>
    /// 错误率
    /// </summary>
    ErrorRate = 4,

    /// <summary>
    /// 数据库熔断
    /// </summary>
    DatabaseCircuitBreaker = 5,

    /// <summary>
    /// 系统异常
    /// </summary>
    SystemException = 6
}
