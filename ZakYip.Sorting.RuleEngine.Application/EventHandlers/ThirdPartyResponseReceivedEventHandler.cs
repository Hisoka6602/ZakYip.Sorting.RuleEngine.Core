using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 第三方API响应接收事件处理器
/// </summary>
public class ThirdPartyResponseReceivedEventHandler : INotificationHandler<ThirdPartyResponseReceivedEvent>
{
    private readonly ILogger<ThirdPartyResponseReceivedEventHandler> _logger;
    private readonly IRuleEngineService _ruleEngineService;
    private readonly ILogRepository _logRepository;
    private readonly IPublisher _publisher;

    public ThirdPartyResponseReceivedEventHandler(
        ILogger<ThirdPartyResponseReceivedEventHandler> logger,
        IRuleEngineService ruleEngineService,
        ILogRepository logRepository,
        IPublisher publisher)
    {
        _logger = logger;
        _ruleEngineService = ruleEngineService;
        _logRepository = logRepository;
        _publisher = publisher;
    }

    public async Task Handle(ThirdPartyResponseReceivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理第三方响应接收事件: ParcelId={ParcelId}, Success={Success}",
            notification.ParcelId, notification.Response.Success);

        await _logRepository.LogInfoAsync(
            $"第三方API响应已接收: {notification.ParcelId}",
            $"成功: {notification.Response.Success}, 消息: {notification.Response.Message}");

        // 执行规则匹配逻辑
        // This will be handled by the orchestration service
        // Event is recorded for audit trail
    }
}
