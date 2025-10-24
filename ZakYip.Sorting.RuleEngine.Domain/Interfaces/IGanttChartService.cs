using ZakYip.Sorting.RuleEngine.Domain.DTOs;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 甘特图服务接口
/// </summary>
public interface IGanttChartService
{
    /// <summary>
    /// 查询指定包裹前后N条数据的甘特图数据
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
}
