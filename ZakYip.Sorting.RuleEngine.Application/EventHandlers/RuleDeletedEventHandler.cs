using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 规则删除事件处理器
/// </summary>
public class RuleDeletedEventHandler : INotificationHandler<RuleDeletedEvent>
{
    private readonly ILogger<RuleDeletedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public RuleDeletedEventHandler(
        ILogger<RuleDeletedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(RuleDeletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理规则删除事件: RuleId={RuleId}, RuleName={RuleName}",
            notification.RuleId, notification.RuleName);

        await _logRepository.LogInfoAsync(
            $"规则已删除: {notification.RuleId}",
            $"规则名称: {notification.RuleName}");
    }
}
