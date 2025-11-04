using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 格口更新事件处理器
/// </summary>
public class ChuteUpdatedEventHandler : INotificationHandler<ChuteUpdatedEvent>
{
    private readonly ILogger<ChuteUpdatedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public ChuteUpdatedEventHandler(
        ILogger<ChuteUpdatedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(ChuteUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理格口更新事件: ChuteId={ChuteId}, ChuteName={ChuteName}, ChuteCode={ChuteCode}",
            notification.ChuteId, notification.ChuteName, notification.ChuteCode);

        await _logRepository.LogInfoAsync(
            $"格口已更新: {notification.ChuteId}",
            $"格口名称: {notification.ChuteName}, 格口编号: {notification.ChuteCode ?? "无"}, 已启用: {notification.IsEnabled}");
    }
}
