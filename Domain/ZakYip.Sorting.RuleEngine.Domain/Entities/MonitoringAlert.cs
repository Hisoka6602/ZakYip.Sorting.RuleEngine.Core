using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;

/// <summary>
/// 监控告警日志实体
/// Monitoring alert log entity
/// </summary>
public class MonitoringAlert : BaseMonitoringAlert
{
    /// <summary>
    /// 额外数据（JSON格式）
    /// Additional data (JSON format)
    /// </summary>
    public string? AdditionalData { get; set; }
}
