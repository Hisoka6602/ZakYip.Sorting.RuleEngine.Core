using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 规则匹配完成事件处理器
/// Handler for rule match completion - sends result to sorting machine
/// </summary>
public class RuleMatchCompletedEventHandler : INotificationHandler<RuleMatchCompletedEvent>
{
    private readonly ILogger<RuleMatchCompletedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public RuleMatchCompletedEventHandler(
        ILogger<RuleMatchCompletedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(RuleMatchCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理规则匹配完成事件: ParcelId={ParcelId}, ChuteNumber={ChuteNumber}, CartCount={CartCount}",
            notification.ParcelId, notification.ChuteNumber, notification.CartCount);

        await _logRepository.LogInfoAsync(
            $"规则匹配已完成: {notification.ParcelId}",
            $"格口号: {notification.ChuteNumber}, 小车号: {notification.CartNumber}, 占用小车数: {notification.CartCount}");

        // 发送结果给分拣程序
        // Send result to sorting machine (will be implemented by ISorterAdapter)
        
        // 关闭包裹处理空间（从缓存删除）
        // Close parcel processing space (remove from cache)
    }
}
