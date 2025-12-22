using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Interfaces;

/// <summary>
/// 包裹信息应用服务接口（Scoped）
/// Parcel information application service interface (Scoped)
/// 
/// 负责包裹信息的业务逻辑和数据访问
/// Responsible for business logic and data access of parcel information
/// </summary>
public interface IParcelInfoAppService
{
    /// <summary>
    /// 更新包裹为自动应答模式
    /// Update parcel to auto-response mode
    /// </summary>
    /// <param name="parcelId">包裹ID / Parcel ID</param>
    /// <param name="targetChute">目标格口 / Target chute</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>是否更新成功 / Whether update was successful</returns>
    Task<bool> UpdateParcelToAutoResponseModeAsync(
        string parcelId, 
        string targetChute, 
        CancellationToken cancellationToken = default);
}
