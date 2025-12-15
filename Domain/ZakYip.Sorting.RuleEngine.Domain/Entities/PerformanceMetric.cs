using ZakYip.Sorting.RuleEngine.Domain.Services;

namespace ZakYip.Sorting.RuleEngine.Domain.Entities;
/// <summary>
/// 性能指标实体
/// </summary>
public class PerformanceMetric
{
    /// <summary>
    /// 指标ID
    /// </summary>
    public string MetricId { get; set; } = Guid.NewGuid().ToString();
    /// 包裹ID（可选）
    public string? ParcelId { get; set; }
    /// 操作名称（如：规则评估、API调用等）
    public string OperationName { get; set; } = string.Empty;
    /// 执行时长（毫秒）
    public long DurationMs { get; set; }
    /// 是否成功
    public bool Success { get; set; }
    /// 错误消息（如果失败）
    public string? ErrorMessage { get; set; }
    /// 额外元数据（JSON格式）
    public string? Metadata { get; set; }
    /// 记录时间
    public DateTime RecordedAt { get; set; } = SystemClockProvider.LocalNow;
}
