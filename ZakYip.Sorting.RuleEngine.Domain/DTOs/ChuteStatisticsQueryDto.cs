namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 格口统计查询参数
/// Chute statistics query parameters
/// </summary>
public class ChuteStatisticsQueryDto
{
    /// <summary>
    /// 格口ID（可选，为空则查询所有格口）
    /// </summary>
    public long? ChuteId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 仅查询启用的格口
    /// </summary>
    public bool OnlyEnabled { get; set; } = true;

    /// <summary>
    /// 按字段排序
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// 排序方向（asc/desc）
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// 页码
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;
}
