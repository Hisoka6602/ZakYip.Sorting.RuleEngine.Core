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

    /// <summary>
    /// 包裹ID（可选）
    /// </summary>
    public string? ParcelId { get; set; }

    /// <summary>
    /// 操作名称（如：规则评估、API调用等）
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 额外元数据（JSON格式）
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// 记录时间
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.Now;
}
