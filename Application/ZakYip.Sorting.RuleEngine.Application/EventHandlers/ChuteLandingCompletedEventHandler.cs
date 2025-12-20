using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 格口落格完成事件处理器（包裹生命终点）
/// Chute landing completed event handler (parcel lifecycle endpoint)
/// </summary>
public class ChuteLandingCompletedEventHandler : INotificationHandler<ChuteLandingCompletedEvent>
{
    private readonly ILogger<ChuteLandingCompletedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly ISystemClock _clock;

    public ChuteLandingCompletedEventHandler(
        ILogger<ChuteLandingCompletedEventHandler> logger,
        ILogRepository logRepository,
        IParcelInfoRepository parcelInfoRepository,
        IParcelLifecycleNodeRepository lifecycleRepository,
        ParcelCacheService cacheService,
        ISystemClock clock)
    {
        _logger = logger;
        _logRepository = logRepository;
        _parcelInfoRepository = parcelInfoRepository;
        _lifecycleRepository = lifecycleRepository;
        _cacheService = cacheService;
        _clock = clock;
    }

    public async Task Handle(ChuteLandingCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理落格完成事件: ParcelId={ParcelId}, ActualChute={ActualChute}",
            notification.ParcelId, notification.ActualChute);

        // 从缓存获取或从数据库加载包裹
        var parcel = await _cacheService.GetOrLoadAsync(
            notification.ParcelId,
            _parcelInfoRepository,
            cancellationToken).ConfigureAwait(false);

        if (parcel == null)
        {
            _logger.LogWarning("未找到包裹: ParcelId={ParcelId}", notification.ParcelId);
            return;
        }

        // 赋值实际落格格口
        parcel.ActualChute = notification.ActualChute;
        
        // 标记为已完成（生命终点）
        parcel.Status = ParcelStatus.Completed;
        parcel.LifecycleStage = ParcelLifecycleStage.Landed;
        parcel.CompletedAt = notification.LandedAt;

        // 添加落格生命周期节点
        var lifecycleNode = new ParcelLifecycleNodeEntity
        {
            ParcelId = parcel.ParcelId,
            Stage = ParcelLifecycleStage.Landed,
            EventTime = notification.LandedAt,
            Description = $"落格完成: 实际格口={notification.ActualChute}, 目标格口={parcel.TargetChute}"
        };

        // 并行执行数据库和缓存操作，互不影响
        // Execute database and cache operations in parallel without waiting for each other
        var dbTask = Task.Run(async () =>
        {
            try
            {
                await _lifecycleRepository.AddAsync(lifecycleNode, cancellationToken).ConfigureAwait(false);
                await _parcelInfoRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库操作失败: ParcelId={ParcelId}", parcel.ParcelId);
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
                _logger.LogError(ex, "缓存操作失败: ParcelId={ParcelId}", parcel.ParcelId);
            }
        }, cancellationToken);

        var logTask = Task.Run(async () =>
        {
            try
            {
                await _logRepository.LogInfoAsync(
                    $"包裹落格完成: {parcel.ParcelId}",
                    $"实际格口: {notification.ActualChute}, 目标格口: {parcel.TargetChute}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志记录失败: ParcelId={ParcelId}", parcel.ParcelId);
            }
        }, cancellationToken);

        // 等待所有操作完成（但不等待彼此）
        // Wait for all operations to complete (but they don't wait for each other)
        await Task.WhenAll(dbTask, cacheTask, logTask).ConfigureAwait(false);
    }
}
