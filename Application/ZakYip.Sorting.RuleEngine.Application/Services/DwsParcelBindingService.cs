using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// DWS包裹绑定服务 - 处理DWS数据接收并绑定到包裹
/// DWS parcel binding service - handles DWS data reception and binds to parcels
/// </summary>
/// <remarks>
/// 此服务复用 DwsDataReceivedEventHandler 的包裹绑定逻辑
/// This service reuses the parcel binding logic from DwsDataReceivedEventHandler
/// </remarks>
public class DwsParcelBindingService
{
    private readonly ILogger<DwsParcelBindingService> _logger;
    private readonly ILogRepository _logRepository;
    private readonly ISystemClock _clock;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly IDwsCommunicationLogRepository _dwsCommunicationLogRepository;

    public DwsParcelBindingService(
        ILogger<DwsParcelBindingService> logger,
        ILogRepository logRepository,
        ISystemClock clock,
        IParcelInfoRepository parcelInfoRepository,
        IParcelLifecycleNodeRepository lifecycleRepository,
        ParcelCacheService cacheService,
        IDwsCommunicationLogRepository dwsCommunicationLogRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
        _clock = clock;
        _parcelInfoRepository = parcelInfoRepository;
        _lifecycleRepository = lifecycleRepository;
        _cacheService = cacheService;
        _dwsCommunicationLogRepository = dwsCommunicationLogRepository;
    }

    /// <summary>
    /// 处理DWS数据接收事件，绑定到包裹
    /// Handle DWS data reception event and bind to parcel
    /// </summary>
    /// <param name="dwsData">DWS数据 / DWS data</param>
    /// <param name="sourceAddress">来源地址 / Source address</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    public async Task HandleDwsDataAsync(DwsData dwsData, string? sourceAddress, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "处理DWS数据接收: ParcelId={ParcelId}, Barcode={Barcode}, Weight={Weight}g",
            dwsData.ParcelId, dwsData.Barcode, dwsData.Weight);

        // ✅ 持久化DWS通信日志（确保数据不丢失）
        // Persist DWS communication log (ensure data is not lost)
        await SaveDwsCommunicationLogAsync(dwsData, sourceAddress, cancellationToken).ConfigureAwait(false);

        // 从缓存获取或从数据库加载包裹
        var parcel = await _cacheService.GetOrLoadAsync(
            dwsData.ParcelId,
            _parcelInfoRepository,
            cancellationToken).ConfigureAwait(false);

        if (parcel == null)
        {
            // 如果包裹不存在，尝试获取最新创建且未赋值DWS的包裹（Barcode为空）
            // If parcel not found, try to get the latest created parcel without DWS data (Barcode is empty)
            parcel = await _parcelInfoRepository.GetLatestWithoutDwsDataAsync(cancellationToken).ConfigureAwait(false);
            
            if (parcel == null)
            {
                _logger.LogWarning("未找到包裹或最新未赋值DWS的包裹: ParcelId={ParcelId}", dwsData.ParcelId);
                await _logRepository.LogWarningAsync(
                    $"DWS数据无法绑定: ParcelId={dwsData.ParcelId}",
                    "未找到等待DWS数据的包裹（无Barcode的包裹）").ConfigureAwait(false);
                return;
            }
            
            _logger.LogInformation(
                "🔗 [步骤2-DWS绑定] DWS数据已绑定到包裹 / DWS data bound to parcel: DwsParcelId={DwsParcelId} → ActualParcelId={ActualParcelId}, Barcode={Barcode}",
                dwsData.ParcelId, parcel.ParcelId, dwsData.Barcode);
            
            await _logRepository.LogInfoAsync(
                $"[DWS绑定] DWS数据已绑定: DwsId={dwsData.ParcelId} → ParcelId={parcel.ParcelId}",
                $"Barcode={dwsData.Barcode}, Weight={dwsData.Weight}g").ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation(
                "✅ [步骤2-DWS绑定] DWS数据已匹配到包裹 / DWS data matched to parcel: ParcelId={ParcelId}, Barcode={Barcode}",
                parcel.ParcelId, dwsData.Barcode);
        }

        // 赋值DWS信息
        parcel.Weight = dwsData.Weight;
        parcel.Volume = dwsData.Volume;
        parcel.Length = dwsData.Length;
        parcel.Width = dwsData.Width;
        parcel.Height = dwsData.Height;
        parcel.Barcode = dwsData.Barcode;
        parcel.LifecycleStage = ParcelLifecycleStage.DwsReceived;

        // 添加DWS接收生命周期节点
        await _lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = parcel.ParcelId,
            Stage = ParcelLifecycleStage.DwsReceived,
            EventTime = _clock.LocalNow,
            Description = $"DWS信息已接收: 重量={parcel.Weight}g, 体积={parcel.Volume}cm³"
        }, cancellationToken).ConfigureAwait(false);

        await _logRepository.LogInfoAsync(
            $"DWS数据已接收: {parcel.ParcelId}",
            $"重量={parcel.Weight}g, 体积={parcel.Volume}cm³").ConfigureAwait(false);

        // 更新包裹到数据库
        await _parcelInfoRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "✅ [DWS绑定完成] 包裹已更新: ParcelId={ParcelId}, Barcode={Barcode}",
            parcel.ParcelId, parcel.Barcode);
    }

    /// <summary>
    /// 持久化DWS通信日志
    /// Persist DWS communication log
    /// </summary>
    private async Task SaveDwsCommunicationLogAsync(DwsData dwsData, string? sourceAddress, CancellationToken cancellationToken)
    {
        try
        {
            await _dwsCommunicationLogRepository.SaveAsync(new DwsCommunicationLog
            {
                Barcode = dwsData.Barcode,
                Weight = dwsData.Weight,
                Volume = dwsData.Volume,
                CommunicationTime = _clock.LocalNow,
                IsSuccess = true
            }, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "DWS通信日志已保存: ParcelId={ParcelId}, Barcode={Barcode}",
                dwsData.ParcelId, dwsData.Barcode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存DWS通信日志失败: ParcelId={ParcelId}", dwsData.ParcelId);
        }
    }
}
