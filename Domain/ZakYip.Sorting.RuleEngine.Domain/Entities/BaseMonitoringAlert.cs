using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 监控告警基类 - 提取MonitoringAlert和MonitoringAlertDto的共享属性
/// Base Monitoring Alert class - Extracts shared properties from MonitoringAlert and MonitoringAlertDto
/// </summary>
/// <remarks>
/// 此抽象基类消除了 MonitoringAlert 和 MonitoringAlertDto 之间的属性重复，
/// 遵循DRY原则。
/// This abstract base class eliminates property duplication between MonitoringAlert 
/// and MonitoringAlertDto, following the DRY principle.
/// </remarks>
public abstract class BaseMonitoringAlert
{
    /// <summary>
    /// 告警ID
    /// Alert ID
    /// </summary>
    public long AlertId { get; set; }

    /// <summary>
    /// 告警类型
    /// Alert type
    /// </summary>
    public AlertType Type { get; set; }

    /// <summary>
    /// 告警级别
    /// Alert severity
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// 告警标题
    /// Alert title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 告警消息
    /// Alert message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 相关资源（如格口ID、包裹ID等）
    /// Related resource (such as chute ID, parcel ID, etc.)
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// 当前值
    /// Current value
    /// </summary>
    public decimal? CurrentValue { get; set; }

    /// <summary>
    /// 阈值
    /// Threshold value
    /// </summary>
    public decimal? ThresholdValue { get; set; }

    /// <summary>
    /// 告警时间
    /// Alert time
    /// </summary>
    public DateTime AlertTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否已解决
    /// Is resolved
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 解决时间
    /// Resolved time
    /// </summary>
    public DateTime? ResolvedTime { get; set; }
}
