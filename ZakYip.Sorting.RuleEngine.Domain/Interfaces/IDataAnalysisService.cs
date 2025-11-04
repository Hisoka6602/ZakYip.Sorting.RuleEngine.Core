using ZakYip.Sorting.RuleEngine.Domain.DTOs;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 数据分析服务接口
/// Data analysis service interface
/// </summary>
public interface IDataAnalysisService
{
    /// <summary>
    /// 获取格口使用热力图数据
    /// Get chute usage heatmap data
    /// </summary>
    Task<List<ChuteHeatmapDto>> GetChuteHeatmapAsync(
        HeatmapQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取分拣效率分析报表
    /// Get sorting efficiency analysis report
    /// </summary>
    Task<SortingEfficiencyOverviewDto> GetSortingEfficiencyReportAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);
}
