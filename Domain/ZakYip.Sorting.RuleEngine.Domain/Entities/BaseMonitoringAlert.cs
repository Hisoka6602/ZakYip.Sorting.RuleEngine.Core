using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Services;

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
    /// 告警类型
    /// Alert type
    public AlertType Type { get; set; }
    /// 告警级别
    /// Alert severity
    public AlertSeverity Severity { get; set; }
    /// 告警标题
    /// Alert title
    public string Title { get; set; } = string.Empty;
    /// 告警消息
    /// Alert message
    public string Message { get; set; } = string.Empty;
    /// 相关资源（如格口ID、包裹ID等）
    /// Related resource (such as chute ID, parcel ID, etc.)
    public string? ResourceId { get; set; }
    /// 当前值
    /// Current value
    public decimal? CurrentValue { get; set; }
    /// 阈值
    /// Threshold value
    public decimal? ThresholdValue { get; set; }
    /// 告警时间
    /// Alert time
    public DateTime AlertTime { get; set; } = SystemClockProvider.LocalNow;
    /// 是否已解决
    /// Is resolved
    public bool IsResolved { get; set; }
    /// 解决时间
    /// Resolved time
    public DateTime? ResolvedTime { get; set; }
}
