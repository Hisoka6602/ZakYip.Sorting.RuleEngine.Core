using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// DWS数据接收事件处理器
/// DWS data received event handler
/// </summary>
public class DwsDataReceivedEventHandler : INotificationHandler<DwsDataReceivedEvent>
{
    private readonly ILogger<DwsDataReceivedEventHandler> _logger;
    private readonly IWcsApiAdapterFactory _apiAdapterFactory;
    private readonly ILogRepository _logRepository;
    private readonly IPublisher _publisher;
    private readonly ISystemClock _clock;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;

    public DwsDataReceivedEventHandler(
        ILogger<DwsDataReceivedEventHandler> logger,
        IWcsApiAdapterFactory apiAdapterFactory,
        ILogRepository logRepository,
        IPublisher publisher,
        ISystemClock clock,
        IParcelInfoRepository parcelInfoRepository,
        IParcelLifecycleNodeRepository lifecycleRepository,
        ParcelCacheService cacheService)
    {
        _logger = logger;
        _apiAdapterFactory = apiAdapterFactory;
        _logRepository = logRepository;
        _publisher = publisher;
        _clock = clock;
        _parcelInfoRepository = parcelInfoRepository;
        _lifecycleRepository = lifecycleRepository;
        _cacheService = cacheService;
    }

    public async Task Handle(DwsDataReceivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理DWS数据接收事件: ParcelId={ParcelId}, Weight={Weight}g",
            notification.ParcelId, notification.DwsData.Weight);

        // 从缓存获取或从数据库加载包裹
        var parcel = await _cacheService.GetOrLoadAsync(
            notification.ParcelId,
            _parcelInfoRepository,
            cancellationToken).ConfigureAwait(false);

        if (parcel == null)
        {
            // 如果包裹不存在，尝试获取最新创建且未赋值DWS的包裹
            parcel = await _parcelInfoRepository.GetLatestWithoutDwsDataAsync(cancellationToken).ConfigureAwait(false);
            
            if (parcel == null)
            {
                _logger.LogWarning("未找到包裹或最新未赋值DWS的包裹: ParcelId={ParcelId}", notification.ParcelId);
                return;
            }
            
            _logger.LogInformation("匹配到最新未赋值DWS的包裹: {ParcelId}", parcel.ParcelId);
        }

        // 赋值DWS信息
        parcel.Weight = notification.DwsData.Weight;
        parcel.Volume = notification.DwsData.Volume;
        parcel.Length = notification.DwsData.Length;
        parcel.Width = notification.DwsData.Width;
        parcel.Height = notification.DwsData.Height;
        parcel.Barcode = notification.DwsData.Barcode;
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
            $"重量: {parcel.Weight}g, 体积: {parcel.Volume}cm³").ConfigureAwait(false);

        // 主动请求格口（主动调用，不发布事件）
        var apiStartTime = _clock.LocalNow;
        try
        {
            var response = await _apiAdapterFactory.GetActiveAdapter().RequestChuteAsync(
                parcel.ParcelId,
                notification.DwsData,
                null, // OcrData not available in this event
                cancellationToken).ConfigureAwait(false);

            // 记录WCS API响应（主动调用的响应，直接记录，不通过事件）
            if (response != null)
            {
                var isSuccess = response.RequestStatus == ApiRequestStatus.Success;
                var message = response.FormattedMessage ?? response.ErrorMessage ?? "无消息";
                
                // 添加API请求生命周期节点
                await _lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
                {
                    ParcelId = parcel.ParcelId,
                    Stage = ParcelLifecycleStage.ApiRequested,
                    EventTime = _clock.LocalNow,
                    Description = $"请求API: 成功={isSuccess}"
                }, cancellationToken).ConfigureAwait(false);

                // 如果API返回了格口信息，更新包裹
                if (isSuccess && !string.IsNullOrEmpty(response.ResponseBody))
                {
                    // 解析目标格口（根据规则引擎或API响应）
                    parcel.TargetChute = ExtractTargetChute(response);
                    parcel.DecisionReason = "API";
                    parcel.LifecycleStage = ParcelLifecycleStage.ChuteAssigned;
                    
                    // 添加格口分配生命周期节点
                    await _lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
                    {
                        ParcelId = parcel.ParcelId,
                        Stage = ParcelLifecycleStage.ChuteAssigned,
                        EventTime = _clock.LocalNow,
                        Description = $"目标格口已分配: {parcel.TargetChute}"
                    }, cancellationToken).ConfigureAwait(false);
                }
                
                await _logRepository.LogInfoAsync(
                    $"WCS API响应已接收: {parcel.ParcelId}",
                    $"成功: {isSuccess}, 消息: {message}").ConfigureAwait(false);
                
                // 发布WCS API调用事件，包含完整的API响应数据
                await _publisher.Publish(new WcsApiCalledEvent
                {
                    ParcelId = parcel.ParcelId,
                    ApiUrl = response.RequestUrl ?? "/api/chute/request",
                    IsSuccess = isSuccess,
                    StatusCode = response.ResponseStatusCode,
                    DurationMs = response.DurationMs,
                    CalledAt = _clock.LocalNow,
                    ErrorMessage = isSuccess ? null : (response.ErrorMessage ?? message),
                    ApiResponse = response
                }, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            var apiDuration = _clock.LocalNow - apiStartTime;
            
            _logger.LogWarning(ex, "WCS API调用失败，将继续使用规则引擎: ParcelId={ParcelId}", parcel.ParcelId);
            await _logRepository.LogWarningAsync(
                $"WCS API调用失败: {parcel.ParcelId}",
                ex.Message).ConfigureAwait(false);
            
            // 发布WCS API调用失败事件
            await _publisher.Publish(new WcsApiCalledEvent
            {
                ParcelId = parcel.ParcelId,
                ApiUrl = "/api/chute/request",
                IsSuccess = false,
                StatusCode = null,
                DurationMs = (long)apiDuration.TotalMilliseconds,
                CalledAt = _clock.LocalNow,
                ErrorMessage = ex.Message,
                ApiResponse = null
            }, cancellationToken).ConfigureAwait(false);
        }

        // 更新包裹信息到数据库
        await _parcelInfoRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);
        
        // 更新缓存
        await _cacheService.SetAsync(parcel, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 从API响应中提取目标格口
    /// Extract target chute from API response
    /// </summary>
    private static string? ExtractTargetChute(WcsApiResponse response)
    {
        // TODO: 根据实际API响应格式解析目标格口
        // Parse target chute based on actual API response format
        return response.ResponseBody;
    }
}
