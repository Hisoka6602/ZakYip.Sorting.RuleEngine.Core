using ZakYip.Sorting.RuleEngine.Domain.DTOs;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 格口统计服务接口
/// Chute statistics service interface
/// </summary>
public interface IChuteStatisticsService
{
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
