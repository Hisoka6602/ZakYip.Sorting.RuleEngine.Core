using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Downstream;
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
    private readonly IDownstreamCommunication _downstreamCommunication;
    private readonly ILogRepository _logRepository;
    private readonly IPublisher _publisher;
    private readonly ISystemClock _clock;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly IDwsCommunicationLogRepository _dwsCommunicationLogRepository;

    public DwsDataReceivedEventHandler(
        ILogger<DwsDataReceivedEventHandler> logger,
        IWcsApiAdapterFactory apiAdapterFactory,
        IDownstreamCommunication downstreamCommunication,
        ILogRepository logRepository,
        IPublisher publisher,
        ISystemClock clock,
        IParcelInfoRepository parcelInfoRepository,
        IParcelLifecycleNodeRepository lifecycleRepository,
        ParcelCacheService cacheService,
        IDwsCommunicationLogRepository dwsCommunicationLogRepository)
    {
        _logger = logger;
        _apiAdapterFactory = apiAdapterFactory;
        _downstreamCommunication = downstreamCommunication;
        _logRepository = logRepository;
        _publisher = publisher;
        _clock = clock;
        _parcelInfoRepository = parcelInfoRepository;
        _lifecycleRepository = lifecycleRepository;
        _cacheService = cacheService;
        _dwsCommunicationLogRepository = dwsCommunicationLogRepository;
    }

    public async Task Handle(DwsDataReceivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理DWS数据接收事件: ParcelId={ParcelId}, Weight={Weight}g",
            notification.ParcelId, notification.DwsData.Weight);

        // ✅ 持久化DWS通信日志（确保数据不丢失）
        // Persist DWS communication log (ensure data is not lost)
        await SaveDwsCommunicationLogAsync(notification.DwsData, notification.SourceAddress, cancellationToken).ConfigureAwait(false);

        // 从缓存获取或从数据库加载包裹
        var parcel = await _cacheService.GetOrLoadAsync(
            notification.ParcelId,
            _parcelInfoRepository,
            cancellationToken).ConfigureAwait(false);

        if (parcel == null)
        {
            // 如果包裹不存在，尝试获取最新创建且未赋值DWS的包裹
            // If parcel not found, try to get the latest created parcel without DWS data
            parcel = await _parcelInfoRepository.GetLatestWithoutDwsDataAsync(cancellationToken).ConfigureAwait(false);
            
            if (parcel == null)
            {
                _logger.LogWarning("未找到包裹或最新未赋值DWS的包裹: ParcelId={ParcelId}", notification.ParcelId);
                await _logRepository.LogWarningAsync(
                    $"DWS数据无法绑定: ParcelId={notification.ParcelId}",
                    "未找到等待DWS数据的包裹").ConfigureAwait(false);
                return;
            }
            
            _logger.LogInformation(
                "🔗 [步骤2-DWS绑定] DWS数据已绑定到包裹 / DWS data bound to parcel: DwsParcelId={DwsParcelId} → ActualParcelId={ActualParcelId}, Barcode={Barcode}",
                notification.ParcelId, parcel.ParcelId, notification.DwsData.Barcode);
            
            await _logRepository.LogInfoAsync(
                $"[DWS绑定] DWS数据已绑定: DwsId={notification.ParcelId} → ParcelId={parcel.ParcelId}",
                $"Barcode={notification.DwsData.Barcode}, Weight={notification.DwsData.Weight}g").ConfigureAwait(false);
        }
        else
        {
            _logger.LogInformation(
                "✅ [步骤2-DWS绑定] DWS数据已匹配到包裹 / DWS data matched to parcel: ParcelId={ParcelId}, Barcode={Barcode}",
                parcel.ParcelId, notification.DwsData.Barcode);
        }

        // 赋值DWS信息
        // Assign DWS information (ensures each DWS data binds to exactly one parcel)
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
                    parcel.SortingMode = Domain.Enums.SortingMode.ApiDriven;  // API驱动模式
                    parcel.LifecycleStage = ParcelLifecycleStage.ChuteAssigned;
                    
                    // 添加格口分配生命周期节点
                    await _lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
                    {
                        ParcelId = parcel.ParcelId,
                        Stage = ParcelLifecycleStage.ChuteAssigned,
                        EventTime = _clock.LocalNow,
                        Description = $"目标格口已分配: {parcel.TargetChute}"
                    }, cancellationToken).ConfigureAwait(false);
                    
                    // 发送格口分配到分拣机 / Send chute assignment to sorter
                    try
                    {
                        if (_downstreamCommunication.IsEnabled)
                        {
                            // 使用 TryParse 安全解析 ParcelId
                            if (!long.TryParse(parcel.ParcelId, out var parcelIdValue))
                            {
                                _logger.LogWarning("解析 ParcelId 失败，输入值无效: {ParcelId}", parcel.ParcelId);
                            }
                            else if (!long.TryParse(parcel.TargetChute, out var chuteIdValue))
                            {
                                _logger.LogWarning("解析 TargetChute 失败，输入值无效: {TargetChute}", parcel.TargetChute);
                            }
                            else
                            {
                                // 构造 ChuteAssignmentNotification 对象
                                var chuteNotification = new ChuteAssignmentNotification
                                {
                                    ParcelId = parcelIdValue,
                                    ChuteId = chuteIdValue,
                                    AssignedAt = _clock.LocalNow
                                };

                                // 序列化为JSON
                                var json = JsonSerializer.Serialize(chuteNotification);

                                // 调用下游通信接口发送
                                await _downstreamCommunication.BroadcastChuteAssignmentAsync(json).ConfigureAwait(false);

                                _logger.LogInformation(
                                    "已发送格口分配到分拣机: ParcelId={ParcelId}, TargetChute={TargetChute}",
                                    parcel.ParcelId, parcel.TargetChute);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "下游通信未配置或已禁用，无法发送格口分配: ParcelId={ParcelId}, TargetChute={TargetChute}",
                                parcel.ParcelId, parcel.TargetChute);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "发送格口分配到分拣机时发生异常: ParcelId={ParcelId}, TargetChute={TargetChute}",
                            parcel.ParcelId, parcel.TargetChute);
                    }
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

        // 并行执行数据库和缓存操作，互不影响
        // Execute database and cache operations in parallel without waiting for each other
        var dbTask = Task.Run(async () =>
        {
            try
            {
                await _parcelInfoRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库更新失败: ParcelId={ParcelId}", parcel.ParcelId);
            }
        }, cancellationToken);

        var cacheTask = Task.Run(async () =>
        {
            try
            {
                await _cacheService.SetAsync(parcel, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "缓存更新失败: ParcelId={ParcelId}", parcel.ParcelId);
            }
        }, cancellationToken);

        // 等待所有操作完成（但不等待彼此）
        // Wait for all operations to complete (but they don't wait for each other)
        await Task.WhenAll(dbTask, cacheTask).ConfigureAwait(false);
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

    /// <summary>
    /// 保存DWS通信日志到数据库（确保持久化）
    /// Save DWS communication log to database (ensure persistence)
    /// </summary>
    private async Task SaveDwsCommunicationLogAsync(DwsData dwsData, string? sourceAddress, CancellationToken cancellationToken)
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
    }
}
