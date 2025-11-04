using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 规则创建事件处理器
/// </summary>
public class RuleCreatedEventHandler : INotificationHandler<RuleCreatedEvent>
{
    private readonly ILogger<RuleCreatedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public RuleCreatedEventHandler(
        ILogger<RuleCreatedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(RuleCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理规则创建事件: RuleId={RuleId}, RuleName={RuleName}, Priority={Priority}",
            notification.RuleId, notification.RuleName, notification.Priority);

        await _logRepository.LogInfoAsync(
            $"规则已创建: {notification.RuleId}",
            $"规则名称: {notification.RuleName}, 目标格口: {notification.TargetChute}, 优先级: {notification.Priority}, 已启用: {notification.IsEnabled}");
    }
}
