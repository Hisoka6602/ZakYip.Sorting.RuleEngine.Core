using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 包裹丢失事件处理器（包裹生命终点）
/// Parcel lost event handler (parcel lifecycle endpoint)
/// </summary>
public class ParcelLostEventHandler : INotificationHandler<ParcelLostEvent>
{
    private readonly ILogger<ParcelLostEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly ISystemClock _clock;

    public ParcelLostEventHandler(
        ILogger<ParcelLostEventHandler> logger,
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

    public async Task Handle(ParcelLostEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogError(
            "处理包裹丢失事件: ParcelId={ParcelId}, AffectedCount={Count}, Reason={Reason}",
            notification.ParcelId, notification.AffectedParcelIds.Count, notification.Reason);

        // 从缓存获取或从数据库加载丢失的包裹
        var parcel = await _cacheService.GetOrLoadAsync(
            notification.ParcelId,
            _parcelInfoRepository,
            cancellationToken).ConfigureAwait(false);

        if (parcel == null)
        {
            _logger.LogWarning("未找到丢失的包裹: ParcelId={ParcelId}", notification.ParcelId);
            return;
        }

        // 标记为丢失（生命终点）
        parcel.Status = ParcelStatus.Lost;
        parcel.LifecycleStage = ParcelLifecycleStage.Lost;
        parcel.CompletedAt = notification.LostAt;

        // 添加丢失生命周期节点
        await _lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = parcel.ParcelId,
            Stage = ParcelLifecycleStage.Lost,
            EventTime = notification.LostAt,
            Description = $"包裹丢失: {notification.Reason ?? "未知原因"}, 受影响包裹数={notification.AffectedParcelIds.Count}"
        }, cancellationToken).ConfigureAwait(false);

        // 更新主包裹信息到数据库
        await _parcelInfoRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);

        // 更新缓存
        await _cacheService.SetAsync(parcel, cancellationToken).ConfigureAwait(false);

        // 处理受影响的包裹
        if (notification.AffectedParcelIds.Count > 0)
        {
            var affectedParcels = new List<ParcelInfo>();
            
            foreach (var affectedId in notification.AffectedParcelIds)
            {
                var affected = await _cacheService.GetOrLoadAsync(
                    affectedId,
                    _parcelInfoRepository,
                    cancellationToken).ConfigureAwait(false);

                if (affected != null)
                {
                    // 更新受影响包裹的状态
                    affected.Status = ParcelStatus.Failed;
                    affected.LifecycleStage = ParcelLifecycleStage.Error;
                    affected.UpdatedAt = notification.LostAt;
                    affectedParcels.Add(affected);

                    // 添加受影响生命周期节点
                    await _lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
                    {
                        ParcelId = affected.ParcelId,
                        Stage = ParcelLifecycleStage.Error,
                        EventTime = notification.LostAt,
                        Description = $"受包裹丢失影响: 丢失包裹={notification.ParcelId}"
                    }, cancellationToken).ConfigureAwait(false);

                    // 更新缓存
                    await _cacheService.SetAsync(affected, cancellationToken).ConfigureAwait(false);
                }
            }

            // 批量更新受影响的包裹
            if (affectedParcels.Count > 0)
            {
                await _parcelInfoRepository.BatchUpdateAsync(affectedParcels, cancellationToken).ConfigureAwait(false);
            }
        }

        await _logRepository.LogErrorAsync(
            $"包裹丢失: {parcel.ParcelId}",
            $"原因: {notification.Reason ?? "未知"}, 受影响包裹数: {notification.AffectedParcelIds.Count}").ConfigureAwait(false);
    }
}
