using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 规则更新事件处理器
/// </summary>
public class RuleUpdatedEventHandler : INotificationHandler<RuleUpdatedEvent>
{
    private readonly ILogger<RuleUpdatedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public RuleUpdatedEventHandler(
        ILogger<RuleUpdatedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(RuleUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理规则更新事件: RuleId={RuleId}, RuleName={RuleName}, Priority={Priority}",
            notification.RuleId, notification.RuleName, notification.Priority);

        await _logRepository.LogInfoAsync(
            $"规则已更新: {notification.RuleId}",
            $"规则名称: {notification.RuleName}, 目标格口: {notification.TargetChute}, 优先级: {notification.Priority}, 已启用: {notification.IsEnabled}").ConfigureAwait(false);
    }
}
