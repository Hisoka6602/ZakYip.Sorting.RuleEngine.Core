using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 格口创建事件处理器
/// </summary>
public class ChuteCreatedEventHandler : INotificationHandler<ChuteCreatedEvent>
{
    private readonly ILogger<ChuteCreatedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public ChuteCreatedEventHandler(
        ILogger<ChuteCreatedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(ChuteCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理格口创建事件: ChuteId={ChuteId}, ChuteName={ChuteName}, ChuteCode={ChuteCode}",
            notification.ChuteId, notification.ChuteName, notification.ChuteCode);

        await _logRepository.LogInfoAsync(
            $"格口已创建: {notification.ChuteId}",
            $"格口名称: {notification.ChuteName}, 格口编号: {notification.ChuteCode ?? "无"}, 已启用: {notification.IsEnabled}");
    }
}
