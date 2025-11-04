using ZakYip.Sorting.RuleEngine.Domain.DTOs;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 监控告警日志实体
/// Monitoring alert log entity
/// </summary>
public class MonitoringAlert
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

    /// <summary>
    /// 额外数据（JSON格式）
    /// </summary>
    public string? AdditionalData { get; set; }
}
