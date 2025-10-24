using ZakYip.Sorting.RuleEngine.Application.DTOs;

namespace ZakYip.Sorting.RuleEngine.Application.Interfaces;

/// <summary>
/// 包裹处理服务接口
/// Parcel processing service interface - main application service
/// </summary>
public interface IParcelProcessingService
{
    /// <summary>
    /// 处理包裹信息
    /// Process parcel information through the complete pipeline
    /// </summary>
    /// <param name="request">包裹处理请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理响应</returns>
    Task<ParcelProcessResponse> ProcessParcelAsync(
        ParcelProcessRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量处理包裹信息
    /// Process multiple parcels in batch
    /// </summary>
    /// <param name="requests">包裹处理请求列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>处理响应列表</returns>
    Task<IEnumerable<ParcelProcessResponse>> ProcessParcelsAsync(
        IEnumerable<ParcelProcessRequest> requests,
        CancellationToken cancellationToken = default);
}
