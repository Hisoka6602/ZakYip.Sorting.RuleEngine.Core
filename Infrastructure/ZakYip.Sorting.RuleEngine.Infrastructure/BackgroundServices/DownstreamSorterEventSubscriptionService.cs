using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 下游分拣机事件订阅服务
/// Downstream sorter event subscription service
/// </summary>
/// <remarks>
/// ⚠️ 这是唯一订阅下游分拣机事件的服务 - 防止影分身 / This is the ONLY service subscribing to downstream sorter events - prevent shadow clones
/// 
/// 职责 / Responsibilities:
/// 1. 订阅 ParcelNotificationReceived 事件 → 创建包裹记录（等待DWS）
/// 2. 订阅 SortingCompletedReceived 事件 → 调用WCS NotifyChuteLanding
/// 3. 所有业务逻辑复用现有EventHandler，不重复实现
/// 
/// 流程 / Flow:
/// ParcelDetected → 创建包裹 → (等待DWS) → DwsDataReceivedEventHandler → WCS API + Rule → 发送格口 → SortingCompleted → WCS NotifyChuteLanding
/// </remarks>
public sealed class DownstreamSorterEventSubscriptionService : IHostedService
{
    private readonly IDownstreamCommunication _downstreamCommunication;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DownstreamSorterEventSubscriptionService> _logger;
    private readonly ISystemClock _clock;

    public DownstreamSorterEventSubscriptionService(
        IDownstreamCommunication downstreamCommunication,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DownstreamSorterEventSubscriptionService> logger,
        ISystemClock clock)
    {
        _downstreamCommunication = downstreamCommunication;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _clock = clock;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 [下游分拣机订阅服务] 启动 / Starting downstream sorter event subscription service");

        _downstreamCommunication.ParcelNotificationReceived += OnParcelDetected;
        _downstreamCommunication.SortingCompletedReceived += OnSortingCompleted;

        _logger.LogInformation("✅ [下游分拣机订阅服务] 已订阅事件 / Subscribed to events");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 [下游分拣机订阅服务] 停止 / Stopping downstream sorter event subscription service");

        _downstreamCommunication.ParcelNotificationReceived -= OnParcelDetected;
        _downstreamCommunication.SortingCompletedReceived -= OnSortingCompleted;

        _logger.LogInformation("✅ [下游分拣机订阅服务] 已取消订阅 / Unsubscribed from events");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 步骤1: 包裹检测 → 创建包裹记录（等待DWS数据）
    /// Step 1: Parcel detected → Create parcel record (waiting for DWS)
    /// </summary>
    private async void OnParcelDetected(object? sender, ParcelNotificationReceivedEventArgs e)
    {
        try
        {
            _logger.LogInformation(
                "📦 [步骤1-包裹检测] ParcelId={ParcelId}, ClientId={ClientId}, ReceivedAt={ReceivedAt}",
                e.ParcelId, e.ClientId, e.ReceivedAt);

            using var scope = _serviceScopeFactory.CreateScope();
            var parcelInfoRepository = scope.ServiceProvider.GetRequiredService<IParcelInfoRepository>();
            var lifecycleRepository = scope.ServiceProvider.GetRequiredService<IParcelLifecycleNodeRepository>();
            var cacheService = scope.ServiceProvider.GetRequiredService<Application.Services.ParcelCacheService>();
            var logRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>();

            var parcelId = e.ParcelId.ToString();

            // 检查包裹是否已存在
            var existingParcel = await parcelInfoRepository.GetByIdAsync(parcelId, CancellationToken.None)
                .ConfigureAwait(false);

            if (existingParcel != null)
            {
                _logger.LogInformation(
                    "ℹ️ [步骤1-包裹检测] 包裹已存在，跳过创建 / Parcel exists, skipping: ParcelId={ParcelId}",
                    parcelId);
                
                await logRepository.LogInfoAsync(
                    $"[下游分拣机] 包裹检测 (已存在): {parcelId}",
                    $"ClientId: {e.ClientId}").ConfigureAwait(false);
                return;
            }

            // 创建包裹记录
            var parcel = new ParcelInfo
            {
                ParcelId = parcelId,
                CreatedAt = _clock.LocalNow,
                LifecycleStage = ParcelLifecycleStage.Created,
                SortingMode = SortingMode.RuleBased
            };

            await parcelInfoRepository.AddAsync(parcel, CancellationToken.None).ConfigureAwait(false);
            await cacheService.SetAsync(parcel, CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation(
                "✅ [步骤1-包裹检测] 包裹已创建 / Parcel created: ParcelId={ParcelId}",
                parcelId);

            // 添加生命周期节点
            await lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
            {
                ParcelId = parcelId,
                Stage = ParcelLifecycleStage.Created,
                EventTime = _clock.LocalNow,
                Description = $"[步骤1] 下游分拣机检测到包裹，ClientId={e.ClientId}，等待DWS数据"
            }, CancellationToken.None).ConfigureAwait(false);

            // 记录到日志文件
            await logRepository.LogInfoAsync(
                $"[下游分拣机] 包裹检测 (新建): {parcelId}",
                $"ClientId: {e.ClientId}, Source: DownstreamSorter, 等待DWS数据").ConfigureAwait(false);

            _logger.LogInformation(
                "⏳ [步骤1-包裹检测] 等待DWS数据 / Waiting for DWS data: ParcelId={ParcelId}",
                parcelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [步骤1-包裹检测] 异常 / Exception: ParcelId={ParcelId}",
                e.ParcelId);
        }
    }

    /// <summary>
    /// 步骤7: 分拣完成 → 调用WCS NotifyChuteLanding
    /// Step 7: Sorting completed → Call WCS NotifyChuteLanding
    /// </summary>
    private async void OnSortingCompleted(object? sender, SortingCompletedReceivedEventArgs e)
    {
        try
        {
            _logger.LogInformation(
                "🎯 [步骤7-分拣完成] ParcelId={ParcelId}, ChuteId={ChuteId}, Success={Success}, Reason={Reason}",
                e.ParcelId, e.ActualChuteId, e.IsSuccess, e.FailureReason);

            using var scope = _serviceScopeFactory.CreateScope();
            var parcelInfoRepository = scope.ServiceProvider.GetRequiredService<IParcelInfoRepository>();
            var lifecycleRepository = scope.ServiceProvider.GetRequiredService<IParcelLifecycleNodeRepository>();
            var apiAdapterFactory = scope.ServiceProvider.GetRequiredService<IWcsApiAdapterFactory>();
            var cacheService = scope.ServiceProvider.GetRequiredService<Application.Services.ParcelCacheService>();
            var logRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>();

            var parcelId = e.ParcelId.ToString();

            // 获取包裹
            var parcel = await parcelInfoRepository.GetByIdAsync(parcelId, CancellationToken.None)
                .ConfigureAwait(false);

            if (parcel == null)
            {
                _logger.LogWarning(
                    "⚠️ [步骤7-分拣完成] 包裹不存在 / Parcel not found: ParcelId={ParcelId}",
                    parcelId);
                
                await logRepository.LogWarningAsync(
                    $"[下游分拣机] 分拣完成-包裹不存在: {parcelId}",
                    $"ChuteId={e.ActualChuteId}, Success={e.IsSuccess}").ConfigureAwait(false);
                return;
            }

            // 更新包裹状态
            parcel.LifecycleStage = e.IsSuccess ? ParcelLifecycleStage.Landed : ParcelLifecycleStage.Timeout;
            parcel.ActualChute = e.ActualChuteId.ToString();
            parcel.CompletedAt = e.CompletedAt.DateTime;

            await parcelInfoRepository.UpdateAsync(parcel, CancellationToken.None).ConfigureAwait(false);
            await cacheService.SetAsync(parcel, CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation(
                "✅ [步骤7-分拣完成] 包裹状态已更新 / Parcel status updated: ParcelId={ParcelId}, Stage={Stage}",
                parcelId, parcel.LifecycleStage);

            // 添加生命周期节点
            await lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
            {
                ParcelId = parcelId,
                Stage = parcel.LifecycleStage,
                EventTime = _clock.LocalNow,
                Description = e.IsSuccess 
                    ? $"[步骤7] 分拣成功，实际格口={e.ActualChuteId}" 
                    : $"[步骤7] 分拣失败，原因={e.FailureReason}"
            }, CancellationToken.None).ConfigureAwait(false);

            // 记录到日志文件
            await logRepository.LogInfoAsync(
                $"[下游分拣机] 分拣完成: {parcelId}",
                $"Success={e.IsSuccess}, TargetChute={parcel.TargetChute}, ActualChute={e.ActualChuteId}, Reason={e.FailureReason}").ConfigureAwait(false);

            // 步骤8: 如果分拣成功，调用WCS NotifyChuteLanding
            if (e.IsSuccess && !string.IsNullOrEmpty(parcel.Barcode))
            {
                try
                {
                    _logger.LogInformation(
                        "📞 [步骤8-WCS落格通知] 开始调用 / Calling WCS NotifyChuteLanding: ParcelId={ParcelId}, ChuteId={ChuteId}, Barcode={Barcode}",
                        parcelId, e.ActualChuteId, parcel.Barcode);

                    var response = await apiAdapterFactory.GetActiveAdapter().NotifyChuteLandingAsync(
                        parcelId,
                        e.ActualChuteId.ToString(),
                        parcel.Barcode,
                        CancellationToken.None).ConfigureAwait(false);

                    if (response?.RequestStatus == ApiRequestStatus.Success)
                    {
                        _logger.LogInformation(
                            "✅ [步骤8-WCS落格通知] 成功 / WCS NotifyChuteLanding succeeded: ParcelId={ParcelId}, Duration={Duration}ms",
                            parcelId, response.DurationMs);

                        await lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
                        {
                            ParcelId = parcelId,
                            Stage = ParcelLifecycleStage.Completed,
                            EventTime = _clock.LocalNow,
                            Description = $"[步骤8] WCS落格通知已发送，耗时={response.DurationMs}ms"
                        }, CancellationToken.None).ConfigureAwait(false);

                        await logRepository.LogInfoAsync(
                            $"[WCS API] NotifyChuteLanding成功: {parcelId}",
                            $"ChuteId={e.ActualChuteId}, Barcode={parcel.Barcode}, Duration={response.DurationMs}ms").ConfigureAwait(false);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ [步骤8-WCS落格通知] 失败 / WCS NotifyChuteLanding failed: ParcelId={ParcelId}, Error={Error}",
                            parcelId, response?.ErrorMessage);

                        await logRepository.LogWarningAsync(
                            $"[WCS API] NotifyChuteLanding失败: {parcelId}",
                            $"Error={response?.ErrorMessage}, StatusCode={response?.ResponseStatusCode}").ConfigureAwait(false);
                    }
                }
                catch (Exception apiEx)
                {
                    _logger.LogError(apiEx,
                        "❌ [步骤8-WCS落格通知] 异常 / Exception calling WCS NotifyChuteLanding: ParcelId={ParcelId}",
                        parcelId);

                    await logRepository.LogErrorAsync(
                        $"[WCS API] NotifyChuteLanding异常: {parcelId}",
                        apiEx.Message).ConfigureAwait(false);
                }
            }
            else if (!e.IsSuccess)
            {
                _logger.LogInformation(
                    "ℹ️ [步骤8-WCS落格通知] 分拣失败，跳过WCS通知 / Sorting failed, skipping WCS notification: ParcelId={ParcelId}",
                    parcelId);
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ [步骤8-WCS落格通知] 缺少Barcode，跳过WCS通知 / Missing barcode, skipping WCS notification: ParcelId={ParcelId}",
                    parcelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [步骤7-分拣完成] 异常 / Exception: ParcelId={ParcelId}",
                e.ParcelId);
        }
    }
}
