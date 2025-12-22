using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 包裹信息应用服务实现（Scoped）
/// Parcel information application service implementation (Scoped)
/// </summary>
public class ParcelInfoAppService : IParcelInfoAppService
{
    private readonly IParcelInfoRepository _parcelRepository;
    private readonly ISystemClock _clock;
    private readonly ILogger<ParcelInfoAppService> _logger;

    public ParcelInfoAppService(
        IParcelInfoRepository parcelRepository,
        ISystemClock clock,
        ILogger<ParcelInfoAppService> logger)
    {
        _parcelRepository = parcelRepository;
        _clock = clock;
        _logger = logger;
    }

    public async Task<bool> UpdateParcelToAutoResponseModeAsync(
        string parcelId, 
        string targetChute, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parcel = await _parcelRepository.GetByIdAsync(parcelId, cancellationToken).ConfigureAwait(false);
            
            if (parcel == null)
            {
                _logger.LogWarning("自动应答：包裹不存在，无法更新分拣模式: ParcelId={ParcelId}", parcelId);
                return false;
            }

            parcel.TargetChute = targetChute;
            parcel.DecisionReason = "AutoResponse";
            parcel.SortingMode = SortingMode.AutoResponse;
            parcel.LifecycleStage = ParcelLifecycleStage.ChuteAssigned;
            parcel.UpdatedAt = _clock.LocalNow;
            
            var success = await _parcelRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);
            
            if (success)
            {
                _logger.LogInformation(
                    "包裹已更新为自动应答模式: ParcelId={ParcelId}, SortingMode={SortingMode}, TargetChute={TargetChute}",
                    parcelId, SortingMode.AutoResponse, targetChute);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新包裹为自动应答模式时发生错误: ParcelId={ParcelId}", parcelId);
            return false;
        }
    }
}
