using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 包裹信息仓储接口
/// Parcel information repository interface
/// </summary>
public interface IParcelInfoRepository
{
    /// <summary>
    /// 根据包裹ID获取包裹信息
    /// Get parcel info by parcel ID
    /// </summary>
    /// <param name="parcelId">包裹ID / Parcel ID</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>包裹信息，不存在则返回null / Parcel info or null if not found</returns>
    Task<ParcelInfo?> GetByIdAsync(string parcelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 添加包裹信息
    /// Add parcel info
    /// </summary>
    /// <param name="parcelInfo">包裹信息 / Parcel info</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>是否成功 / Success indicator</returns>
    Task<bool> AddAsync(ParcelInfo parcelInfo, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新包裹信息
    /// Update parcel info
    /// </summary>
    /// <param name="parcelInfo">包裹信息 / Parcel info</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>是否成功 / Success indicator</returns>
    Task<bool> UpdateAsync(ParcelInfo parcelInfo, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量更新包裹信息
    /// Batch update parcel infos
    /// </summary>
    /// <param name="parcelInfos">包裹信息列表 / Parcel info list</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>更新成功的数量 / Number of successfully updated items</returns>
    Task<int> BatchUpdateAsync(IEnumerable<ParcelInfo> parcelInfos, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 搜索包裹信息（分页）
    /// Search parcel infos (paginated)
    /// </summary>
    /// <param name="status">状态过滤 / Status filter (optional)</param>
    /// <param name="lifecycleStage">生命周期阶段过滤 / Lifecycle stage filter (optional)</param>
    /// <param name="bagId">袋ID过滤 / Bag ID filter (optional)</param>
    /// <param name="startTime">开始时间过滤 / Start time filter (optional)</param>
    /// <param name="endTime">结束时间过滤 / End time filter (optional)</param>
    /// <param name="page">页码（从1开始）/ Page number (1-based)</param>
    /// <param name="pageSize">每页大小 / Page size</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>包裹信息列表和总数 / Parcel info list and total count</returns>
    Task<(IReadOnlyList<ParcelInfo> Items, int TotalCount)> SearchAsync(
        ParcelStatus? status = null,
        ParcelLifecycleStage? lifecycleStage = null,
        string? bagId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取最新创建且未赋值DWS信息的包裹
    /// Get the latest created parcel without DWS data
    /// </summary>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>包裹信息，不存在则返回null / Parcel info or null if not found</returns>
    Task<ParcelInfo?> GetLatestWithoutDwsDataAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据袋ID获取包裹列表
    /// Get parcels by bag ID
    /// </summary>
    /// <param name="bagId">袋ID / Bag ID</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>包裹信息列表 / Parcel info list</returns>
    Task<IReadOnlyList<ParcelInfo>> GetByBagIdAsync(string bagId, CancellationToken cancellationToken = default);
}
