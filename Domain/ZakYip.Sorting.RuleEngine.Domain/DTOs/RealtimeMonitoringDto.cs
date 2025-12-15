using ZakYip.Sorting.RuleEngine.Domain.Services;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

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
    public DateTime UpdateTime { get; set; } = SystemClockProvider.LocalNow;
}
