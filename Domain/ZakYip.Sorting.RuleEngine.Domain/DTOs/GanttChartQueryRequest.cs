namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 甘特图数据查询请求
/// </summary>
public class GanttChartQueryRequest
{
    /// <summary>
    /// 目标包裹ID或条码
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// 查询目标前面N条数据
    /// </summary>
    public int BeforeCount { get; set; } = 5;

    /// <summary>
    /// 查询目标后面N条数据
    /// </summary>
    public int AfterCount { get; set; } = 5;
}
