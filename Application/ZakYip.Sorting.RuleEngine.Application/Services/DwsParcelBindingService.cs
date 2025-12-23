using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// DWS包裹绑定服务 - 处理DWS数据接收并绑定到包裹
/// DWS parcel binding service - handles DWS data reception and binds to parcels
/// </summary>
/// <remarks>
/// 此服务负责：
/// 1. 持久化DWS通信日志
/// 2. 绑定DWS数据到包裹（ParcelId为空时自动查找）
/// 3. 发布DwsDataReceivedEvent给MediatR，触发完整业务流程（规则引擎+WCS+格口分配）
/// 
/// This service is responsible for:
/// 1. Persisting DWS communication log
/// 2. Binding DWS data to parcel (auto-find when ParcelId is empty)
/// 3. Publishing DwsDataReceivedEvent to MediatR to trigger complete business flow (rule engine + WCS + chute assignment)
/// </remarks>
public class DwsParcelBindingService
{
    private readonly ILogger<DwsParcelBindingService> _logger;
    private readonly IPublisher _publisher;
    private readonly ILogRepository _logRepository;
    private readonly ISystemClock _clock;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly IDwsCommunicationLogRepository _dwsCommunicationLogRepository;

    public DwsParcelBindingService(
        ILogger<DwsParcelBindingService> logger,
        IPublisher publisher,
        ILogRepository logRepository,
        ISystemClock clock,
        IParcelInfoRepository parcelInfoRepository,
        IParcelLifecycleNodeRepository lifecycleRepository,
        ParcelCacheService cacheService,
        IDwsCommunicationLogRepository dwsCommunicationLogRepository)
    {
        _logger = logger;
        _publisher = publisher;
        _logRepository = logRepository;
        _clock = clock;
        _parcelInfoRepository = parcelInfoRepository;
        _lifecycleRepository = lifecycleRepository;
        _cacheService = cacheService;
        _dwsCommunicationLogRepository = dwsCommunicationLogRepository;
    }

    /// <summary>
    /// 处理DWS数据接收事件，绑定到包裹并触发完整业务流程
    /// Handle DWS data reception event, bind to parcel and trigger complete business flow
    /// </summary>
    /// <param name="dwsData">DWS数据 / DWS data</param>
    /// <param name="sourceAddress">来源地址 / Source address</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    public async Task HandleDwsDataAsync(DwsData dwsData, string? sourceAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "📦 [步骤1-DWS接收] 处理DWS数据: ParcelId={ParcelId}, Barcode={Barcode}, Weight={Weight}g",
                dwsData.ParcelId, dwsData.Barcode, dwsData.Weight);

            // ✅ 持久化DWS通信日志（确保数据不丢失，即使后续流程失败）
            // Persist DWS communication log (ensure data is not lost even if subsequent process fails)
            await SaveDwsCommunicationLogAsync(dwsData, sourceAddress, cancellationToken).ConfigureAwait(false);

            // 🔍 智能包裹绑定：ParcelId为空时自动查找最新未绑定包裹
            // Smart parcel binding: auto-find latest unbound parcel when ParcelId is empty
            string? parcelId = await FindOrBindParcelIdAsync(dwsData, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(parcelId))
            {
                _logger.LogWarning(
                    "⚠️ [步骤1-DWS接收] 无法绑定DWS数据，未找到待绑定包裹: Barcode={Barcode}",
                    dwsData.Barcode);
                await _logRepository.LogWarningAsync(
                    $"DWS数据无法绑定: Barcode={dwsData.Barcode}",
                    "未找到待绑定的包裹。包裹必须由下游分拣机预先创建。").ConfigureAwait(false);
                return;
            }

            _logger.LogInformation(
                "🔗 [步骤2-包裹绑定] DWS数据已绑定到包裹 / DWS data bound to parcel: ParcelId={ParcelId}, Barcode={Barcode}",
                parcelId, dwsData.Barcode);

            // ✅ 发布 DwsDataReceivedEvent 给 MediatR，触发完整业务流程：
            // - DwsDataReceivedEventHandler: 更新包裹信息 → 规则引擎匹配 → WCS请求格口 → 发送格口给分拣机
            // Publish DwsDataReceivedEvent to MediatR to trigger complete business flow:
            // - DwsDataReceivedEventHandler: Update parcel info → Rule engine matching → WCS request chute → Send chute to sorter
            await _publisher.Publish(new DwsDataReceivedEvent
            {
                ParcelId = parcelId,
                DwsData = dwsData,
                ReceivedAt = _clock.LocalNow,
                SourceAddress = sourceAddress
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "📢 [步骤2-事件发布] DwsDataReceivedEvent 已发布 / Event published: ParcelId={ParcelId}",
                parcelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [DWS数据处理] 异常 / Exception: Barcode={Barcode}",
                dwsData.Barcode);
            
            // ⚠️ 即使发生异常，也记录警告日志，确保异常不影响其他DWS数据处理
            // Even if exception occurs, log warning to ensure it doesn't affect other DWS data processing
            try
            {
                await _logRepository.LogErrorAsync(
                    $"DWS数据处理异常: Barcode={dwsData.Barcode}",
                    ex.Message).ConfigureAwait(false);
            }
            catch
            {
                // 忽略日志记录失败 / Ignore log recording failure
            }
        }
    }

    /// <summary>
    /// 查找或绑定包裹ID
    /// Find or bind parcel ID
    /// </summary>
    /// <param name="dwsData">DWS数据 / DWS data</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>包裹ID，如果未找到则返回null / Parcel ID, or null if not found</returns>
    private async Task<string?> FindOrBindParcelIdAsync(DwsData dwsData, CancellationToken cancellationToken)
    {
        try
        {
            // 场景1: DWS数据中包含ParcelId，直接使用
            // Scenario 1: DWS data contains ParcelId, use it directly
            if (!string.IsNullOrEmpty(dwsData.ParcelId))
            {
                _logger.LogDebug(
                    "DWS数据包含ParcelId: {ParcelId}",
                    dwsData.ParcelId);
                return dwsData.ParcelId;
            }

            // 场景2: DWS数据中没有ParcelId（常见情况），查找最新未绑定Barcode的包裹
            // Scenario 2: DWS data doesn't contain ParcelId (common case), find latest parcel without Barcode
            _logger.LogInformation(
                "DWS数据不包含ParcelId，尝试查找最新未绑定包裹: Barcode={Barcode}",
                dwsData.Barcode);

            var latestParcel = await _parcelInfoRepository
                .GetLatestWithoutDwsDataAsync(cancellationToken)
                .ConfigureAwait(false);

            if (latestParcel != null)
            {
                _logger.LogInformation(
                    "✅ 找到最新未绑定包裹: ParcelId={ParcelId}，将绑定到 Barcode={Barcode}",
                    latestParcel.ParcelId, dwsData.Barcode);
                return latestParcel.ParcelId;
            }

            _logger.LogWarning(
                "⚠️ 未找到未绑定的包裹，无法绑定DWS数据: Barcode={Barcode}",
                dwsData.Barcode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 查找包裹ID失败: Barcode={Barcode}",
                dwsData.Barcode);
            
            // 数据库异常不应阻止DWS数据接收，返回null并记录警告
            // Database exception should not block DWS data reception, return null and log warning
            return null;
        }
    }

    /// <summary>
    /// 持久化DWS通信日志（确保数据不丢失，即使后续流程失败）
    /// Persist DWS communication log (ensure data is not lost even if subsequent process fails)
    /// </summary>
    private async Task SaveDwsCommunicationLogAsync(DwsData dwsData, string? sourceAddress, CancellationToken cancellationToken)
    {
        try
        {
            var log = new DwsCommunicationLog
            {
                CommunicationType = CommunicationType.Tcp,
                DwsAddress = sourceAddress ?? "未知DWS地址 / Unknown DWS Address",
                OriginalContent = JsonSerializer.Serialize(dwsData),
                FormattedContent = JsonSerializer.Serialize(dwsData, new JsonSerializerOptions { WriteIndented = true }),
                Barcode = dwsData.Barcode,
                Weight = dwsData.Weight,
                Volume = dwsData.Volume,
                ImagesJson = dwsData.Images != null && dwsData.Images.Any() 
                    ? JsonSerializer.Serialize(dwsData.Images) 
                    : null,
                CommunicationTime = _clock.LocalNow,
                IsSuccess = true,
                ErrorMessage = null
            };

            await _dwsCommunicationLogRepository.SaveAsync(log, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "DWS通信日志已保存: Barcode={Barcode}, Weight={Weight}g",
                dwsData.Barcode, dwsData.Weight);
        }
        catch (Exception ex)
        {
            // ⚠️ 持久化失败不应阻止DWS数据处理，仅记录错误
            // Persistence failure should not block DWS data processing, just log error
            _logger.LogError(ex,
                "❌ 保存DWS通信日志失败: Barcode={Barcode}",
                dwsData.Barcode);
        }
    }
}
