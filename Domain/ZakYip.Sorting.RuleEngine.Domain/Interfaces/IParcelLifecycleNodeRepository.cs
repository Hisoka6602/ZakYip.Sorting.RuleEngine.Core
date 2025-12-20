using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 包裹生命周期节点仓储接口
/// Parcel lifecycle node repository interface
/// </summary>
public interface IParcelLifecycleNodeRepository
{
    /// <summary>
    /// 添加生命周期节点
    /// Add lifecycle node
    /// </summary>
    /// <param name="node">生命周期节点 / Lifecycle node</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>是否成功 / Success indicator</returns>
    Task<bool> AddAsync(ParcelLifecycleNodeEntity node, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量添加生命周期节点
    /// Batch add lifecycle nodes
    /// </summary>
    /// <param name="nodes">生命周期节点列表 / Lifecycle node list</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>添加成功的数量 / Number of successfully added items</returns>
    Task<int> BatchAddAsync(IEnumerable<ParcelLifecycleNodeEntity> nodes, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据包裹ID获取生命周期节点列表（按时间倒序）
    /// Get lifecycle nodes by parcel ID (ordered by time descending)
    /// </summary>
    /// <param name="parcelId">包裹ID / Parcel ID</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>生命周期节点列表 / Lifecycle node list</returns>
    Task<IReadOnlyList<ParcelLifecycleNodeEntity>> GetByParcelIdAsync(string parcelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据时间范围获取生命周期节点（分页）
    /// Get lifecycle nodes by time range (paginated)
    /// </summary>
    /// <param name="startTime">开始时间 / Start time</param>
    /// <param name="endTime">结束时间 / End time</param>
    /// <param name="stage">阶段过滤（可选）/ Stage filter (optional)</param>
    /// <param name="page">页码（从1开始）/ Page number (1-based)</param>
    /// <param name="pageSize">每页大小 / Page size</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>生命周期节点列表和总数 / Lifecycle node list and total count</returns>
    Task<(IReadOnlyList<ParcelLifecycleNodeEntity> Items, int TotalCount)> GetByTimeRangeAsync(
        DateTime startTime,
        DateTime endTime,
        ParcelLifecycleStage? stage = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
}
