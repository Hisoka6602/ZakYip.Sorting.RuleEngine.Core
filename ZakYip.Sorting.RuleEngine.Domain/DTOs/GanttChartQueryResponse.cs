namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 甘特图数据查询响应
/// </summary>
public class GanttChartQueryResponse
{
    /// <summary>
    /// 甘特图数据项列表
    /// </summary>
    public List<GanttChartDataItem> Items { get; set; } = new();

    /// <summary>
    /// 目标包裹ID
    /// </summary>
    public string? TargetParcelId { get; set; }

    /// <summary>
    /// 目标包裹在列表中的索引
    /// </summary>
    public int? TargetIndex { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 查询是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
