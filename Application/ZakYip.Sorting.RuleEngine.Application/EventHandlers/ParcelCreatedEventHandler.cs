using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 包裹创建事件处理器
/// Parcel created event handler
/// </summary>
public class ParcelCreatedEventHandler : INotificationHandler<ParcelCreatedEvent>
{
    private readonly ILogger<ParcelCreatedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly ISystemClock _clock;

    public ParcelCreatedEventHandler(
        ILogger<ParcelCreatedEventHandler> logger,
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

    public async Task Handle(ParcelCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理包裹创建事件: ParcelId={ParcelId}, CartNumber={CartNumber}, Sequence={Sequence}",
            notification.ParcelId, notification.CartNumber, notification.SequenceNumber);

        // 创建包裹信息
        var parcel = new ParcelInfo
        {
            ParcelId = notification.ParcelId,
            CartNumber = notification.CartNumber,
            Barcode = notification.Barcode,
            Status = ParcelStatus.Pending,
            LifecycleStage = ParcelLifecycleStage.Created,
            CreatedAt = notification.CreatedAt
        };

        // 创建生命周期节点
        var lifecycleNode = new ParcelLifecycleNodeEntity
        {
            ParcelId = notification.ParcelId,
            Stage = ParcelLifecycleStage.Created,
            EventTime = notification.CreatedAt,
            Description = $"包裹创建: 小车号={notification.CartNumber}"
        };

        // 并行执行数据库和缓存操作，互不影响
        // Execute database and cache operations in parallel without waiting for each other
        var dbTask = Task.Run(async () =>
        {
            try
            {
                var addResult = await _parcelInfoRepository.AddAsync(parcel, cancellationToken).ConfigureAwait(false);
                if (!addResult)
                {
                    _logger.LogError("包裹信息持久化失败: ParcelId={ParcelId}", notification.ParcelId);
                }
                
                await _lifecycleRepository.AddAsync(lifecycleNode, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库操作失败: ParcelId={ParcelId}", notification.ParcelId);
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
                _logger.LogError(ex, "缓存操作失败: ParcelId={ParcelId}", notification.ParcelId);
            }
        }, cancellationToken);

        var logTask = Task.Run(async () =>
        {
            try
            {
                await _logRepository.LogInfoAsync(
                    $"包裹已创建: {notification.ParcelId}",
                    $"小车号: {notification.CartNumber}, 序号: {notification.SequenceNumber}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志记录失败: ParcelId={ParcelId}", notification.ParcelId);
            }
        }, cancellationToken);

        // 等待所有操作完成（但不等待彼此）
        // Wait for all operations to complete (but they don't wait for each other)
        await Task.WhenAll(dbTask, cacheTask, logTask).ConfigureAwait(false);
    }
}
