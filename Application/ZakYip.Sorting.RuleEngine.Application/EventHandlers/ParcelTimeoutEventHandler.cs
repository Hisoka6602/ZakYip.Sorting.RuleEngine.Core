using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 包裹超时事件处理器（包裹生命终点）
/// Parcel timeout event handler (parcel lifecycle endpoint)
/// </summary>
public class ParcelTimeoutEventHandler : INotificationHandler<ParcelTimeoutEvent>
{
    private readonly ILogger<ParcelTimeoutEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly ISystemClock _clock;

    public ParcelTimeoutEventHandler(
        ILogger<ParcelTimeoutEventHandler> logger,
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

    public async Task Handle(ParcelTimeoutEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "处理包裹超时事件: ParcelId={ParcelId}, Reason={Reason}",
            notification.ParcelId, notification.Reason);

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

        // 标记为超时（生命终点）
        parcel.Status = ParcelStatus.Timeout;
        parcel.LifecycleStage = ParcelLifecycleStage.Timeout;
        parcel.CompletedAt = notification.TimeoutAt;

        // 添加超时生命周期节点
        var lifecycleNode = new ParcelLifecycleNodeEntity
        {
            ParcelId = parcel.ParcelId,
            Stage = ParcelLifecycleStage.Timeout,
            EventTime = notification.TimeoutAt,
            Description = $"包裹超时: {notification.Reason ?? "未知原因"}"
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
                await _logRepository.LogWarningAsync(
                    $"包裹超时: {parcel.ParcelId}",
                    $"原因: {notification.Reason ?? "未知"}").ConfigureAwait(false);
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
