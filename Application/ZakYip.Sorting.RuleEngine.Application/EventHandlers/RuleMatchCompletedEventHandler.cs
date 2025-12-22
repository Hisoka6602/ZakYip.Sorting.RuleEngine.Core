using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 规则匹配完成事件处理器
/// Rule match completed event handler
/// </summary>
public class RuleMatchCompletedEventHandler : INotificationHandler<RuleMatchCompletedEvent>
{
    private readonly ILogger<RuleMatchCompletedEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly ISorterAdapterManager _sorterAdapterManager;
    private readonly IParcelInfoRepository _parcelRepository;

    public RuleMatchCompletedEventHandler(
        ILogger<RuleMatchCompletedEventHandler> logger,
        ILogRepository logRepository,
        ISorterAdapterManager sorterAdapterManager,
        IParcelInfoRepository parcelRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
        _sorterAdapterManager = sorterAdapterManager;
        _parcelRepository = parcelRepository;
    }

    public async Task Handle(RuleMatchCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理规则匹配完成事件: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}, CartCount={CartCount}",
            notification.ParcelId, notification.ChuteNumber, notification.CartCount);

        await _logRepository.LogInfoAsync(
            $"规则匹配已完成: {notification.ParcelId}",
            $"格口号: {notification.ChuteNumber}, 小车号: {notification.CartNumber}, 占用小车数: {notification.CartCount}").ConfigureAwait(false);

        // 更新包裹信息，标记为规则分拣模式 / Update parcel info, mark as rule-based sorting mode
        try
        {
            var parcel = await _parcelRepository.GetByIdAsync(notification.ParcelId, cancellationToken).ConfigureAwait(false);
            if (parcel != null)
            {
                parcel.TargetChute = notification.ChuteNumber;
                parcel.DecisionReason = "RuleEngine";
                parcel.SortingMode = SortingMode.RuleBased;  // 规则分拣模式
                parcel.LifecycleStage = ParcelLifecycleStage.ChuteAssigned;
                
                await _parcelRepository.UpdateAsync(parcel, cancellationToken).ConfigureAwait(false);
                
                _logger.LogInformation(
                    "包裹已更新为规则分拣模式: ParcelId={ParcelId}, SortingMode={SortingMode}",
                    notification.ParcelId, SortingMode.RuleBased);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新包裹分拣模式失败: ParcelId={ParcelId}", notification.ParcelId);
        }

        // 发送格口号到下游分拣机系统
        // Send chute number to downstream sorter system
        try
        {
            var success = await _sorterAdapterManager.SendChuteNumberAsync(
                notification.ParcelId,
                notification.ChuteNumber,
                cancellationToken).ConfigureAwait(false);

            if (success)
            {
                _logger.LogInformation(
                    "格口号已发送到下游分拣机: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}",
                    notification.ParcelId, notification.ChuteNumber);
                await _logRepository.LogInfoAsync(
                    $"格口号已发送: {notification.ParcelId}",
                    $"格口号: {notification.ChuteNumber}").ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning(
                    "格口号发送失败: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}",
                    notification.ParcelId, notification.ChuteNumber);
                await _logRepository.LogWarningAsync(
                    $"格口号发送失败: {notification.ParcelId}",
                    $"格口号: {notification.ChuteNumber}").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "发送格口号到下游分拣机时发生异常: ParcelId={ParcelId}",
                notification.ParcelId);
            await _logRepository.LogErrorAsync(
                $"发送格口号异常: {notification.ParcelId}",
                ex.Message).ConfigureAwait(false);
        }
        
        // 关闭包裹处理空间（从缓存删除）
        // Close parcel processing space (remove from cache)
        // Note: This is handled by ParcelOrchestrationService
    }
}
