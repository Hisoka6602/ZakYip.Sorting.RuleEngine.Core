using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 包裹集包事件处理器（后续扩展准备）
/// Parcel bagging event handler (for future extension)
/// </summary>
public class ParcelBaggedEventHandler : INotificationHandler<ParcelBaggedEvent>
{
    private readonly ILogger<ParcelBaggedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly IParcelInfoRepository _parcelInfoRepository;
    private readonly IParcelLifecycleNodeRepository _lifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly ISystemClock _clock;

    public ParcelBaggedEventHandler(
        ILogger<ParcelBaggedEventHandler> logger,
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

    public async Task Handle(ParcelBaggedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理集包事件: ParcelId={ParcelId}, BagId={BagId}, Operator={Operator}",
            notification.ParcelId, notification.BagId, notification.Operator);

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

        // 赋值袋ID
        parcel.BagId = notification.BagId;
        parcel.LifecycleStage = ParcelLifecycleStage.Bagged;

        // 添加集包生命周期节点
        await _lifecycleRepository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = parcel.ParcelId,
            Stage = ParcelLifecycleStage.Bagged,
            EventTime = notification.BaggedAt,
            Description = $"集包完成: 袋ID={notification.BagId}, 操作员={notification.Operator ?? "系统"}"
        }, cancellationToken).ConfigureAwait(false);

        // 更新包裹信息到数据库
        await _parcelInfoRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);

        // 更新缓存
        await _cacheService.SetAsync(parcel, cancellationToken).ConfigureAwait(false);

        await _logRepository.LogInfoAsync(
            $"包裹集包完成: {parcel.ParcelId}",
            $"袋ID: {notification.BagId}, 操作员: {notification.Operator ?? "系统"}").ConfigureAwait(false);
    }
}
