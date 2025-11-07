using ZakYip.Sorting.RuleEngine.Domain.DTOs;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 数据分析服务接口
/// Data analysis service interface
/// 包含格口使用热力图、分拣效率分析、甘特图数据查询和格口统计功能
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

    /// <summary>
    /// 查询指定包裹前后N条数据的甘特图数据
    /// Query Gantt chart data for N records before and after the specified parcel
    /// </summary>
    /// <param name="target">目标包裹ID或条码</param>
    /// <param name="beforeCount">查询目标前面N条数据</param>
    /// <param name="afterCount">查询目标后面N条数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>甘特图数据查询响应</returns>
    Task<GanttChartQueryResponse> QueryGanttChartDataAsync(
        string target,
        int beforeCount,
        int afterCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取格口利用率统计
    /// Get chute utilization statistics
    /// </summary>
    Task<List<ChuteUtilizationStatisticsDto>> GetChuteUtilizationStatisticsAsync(
        ChuteStatisticsQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个格口的统计信息
    /// Get statistics for a single chute
    /// </summary>
    Task<ChuteUtilizationStatisticsDto?> GetChuteStatisticsByIdAsync(
        long chuteId,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取分拣效率概览
    /// Get sorting efficiency overview
    /// </summary>
    Task<SortingEfficiencyOverviewDto> GetSortingEfficiencyOverviewAsync(
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取格口小时级统计（用于趋势分析）
    /// Get hourly chute statistics for trend analysis
    /// </summary>
    Task<List<ChuteHourlyStatisticsDto>> GetChuteHourlyStatisticsAsync(
        long chuteId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default);
}
