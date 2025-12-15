using ZakYip.Sorting.RuleEngine.Domain.Services;
namespace ZakYip.Sorting.RuleEngine.Domain.DTOs;

/// <summary>
/// 热力图查询参数
/// Heatmap query parameters
/// </summary>
public class HeatmapQueryDto
{
    /// <summary>
    /// 开始日期
    /// </summary>
    public DateTime StartDate { get; set; } = SystemClockProvider.LocalNow.Date.AddDays(-7);

    /// <summary>
    /// 结束日期
    /// </summary>
    public DateTime EndDate { get; set; } = SystemClockProvider.LocalNow.Date;

    /// <summary>
    /// 格口ID（可选，为空则查询所有格口）
    /// </summary>
    public long? ChuteId { get; set; }

    /// <summary>
    /// 仅查询启用的格口
    /// </summary>
    public bool OnlyEnabled { get; set; } = true;
}
