namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 监控告警数据传输对象
/// Monitoring alert data transfer object
/// </summary>
public class MonitoringAlertDto
{
    /// <summary>
    /// 告警ID
    /// </summary>
    public string AlertId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 告警类型
    /// </summary>
    public AlertType Type { get; set; }

    /// <summary>
    /// 告警级别
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// 告警标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 告警消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 相关资源（如格口ID、包裹ID等）
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// 当前值
    /// </summary>
    public decimal? CurrentValue { get; set; }

    /// <summary>
    /// 阈值
    /// </summary>
    public decimal? ThresholdValue { get; set; }

    /// <summary>
    /// 告警时间
    /// </summary>
    public DateTime AlertTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否已解决
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 解决时间
    /// </summary>
    public DateTime? ResolvedTime { get; set; }
}

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

/// <summary>
/// 告警级别
/// Alert severity
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// 信息
    /// </summary>
    Info = 1,

    /// <summary>
    /// 警告
    /// </summary>
    Warning = 2,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 3,

    /// <summary>
    /// 严重
    /// </summary>
    Critical = 4
}

/// <summary>
/// 实时监控数据
/// Real-time monitoring data
/// </summary>
public class RealtimeMonitoringDto
{
    /// <summary>
    /// 当前包裹处理速率（包裹/分钟）
    /// </summary>
    public decimal CurrentProcessingRate { get; set; }

    /// <summary>
    /// 当前活跃格口数
    /// </summary>
    public int ActiveChutes { get; set; }

    /// <summary>
    /// 平均格口使用率
    /// </summary>
    public decimal AverageChuteUsageRate { get; set; }

    /// <summary>
    /// 当前错误率（%）
    /// </summary>
    public decimal CurrentErrorRate { get; set; }

    /// <summary>
    /// 数据库状态
    /// </summary>
    public DatabaseStatus DatabaseStatus { get; set; }

    /// <summary>
    /// 最近1分钟包裹数
    /// </summary>
    public long LastMinuteParcels { get; set; }

    /// <summary>
    /// 最近5分钟包裹数
    /// </summary>
    public long Last5MinutesParcels { get; set; }

    /// <summary>
    /// 最近1小时包裹数
    /// </summary>
    public long LastHourParcels { get; set; }

    /// <summary>
    /// 活跃告警数
    /// </summary>
    public int ActiveAlerts { get; set; }

    /// <summary>
    /// 系统健康状态
    /// </summary>
    public SystemHealthStatus HealthStatus { get; set; }

    /// <summary>
    /// 数据更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}

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

/// <summary>
/// 系统健康状态
/// </summary>
public enum SystemHealthStatus
{
    /// <summary>
    /// 健康
    /// </summary>
    Healthy = 1,

    /// <summary>
    /// 警告
    /// </summary>
    Warning = 2,

    /// <summary>
    /// 不健康
    /// </summary>
    Unhealthy = 3,

    /// <summary>
    /// 严重
    /// </summary>
    Critical = 4
}
