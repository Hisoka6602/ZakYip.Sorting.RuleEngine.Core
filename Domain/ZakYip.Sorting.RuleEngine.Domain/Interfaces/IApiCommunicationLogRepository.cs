using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// API通信日志仓储接口
/// API Communication Log Repository Interface
/// </summary>
public interface IApiCommunicationLogRepository
{
    /// <summary>
    /// 保存API通信日志
    /// Save API communication log
    /// </summary>
    /// <param name="log">API通信日志实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveAsync(ApiCommunicationLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量保存API通信日志
    /// Save multiple API communication logs
    /// </summary>
    /// <param name="logs">API通信日志列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SaveManyAsync(IEnumerable<ApiCommunicationLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定包裹的API通信日志
    /// Get API communication logs for a specific parcel
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<List<ApiCommunicationLog>> GetByParcelIdAsync(string parcelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定时间范围内的API通信日志
    /// Get API communication logs within a time range
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<List<ApiCommunicationLog>> GetByTimeRangeAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
