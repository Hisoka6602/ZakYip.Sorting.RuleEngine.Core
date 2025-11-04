using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 格口删除事件处理器
/// </summary>
public class ChuteDeletedEventHandler : INotificationHandler<ChuteDeletedEvent>
{
    private readonly ILogger<ChuteDeletedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public ChuteDeletedEventHandler(
        ILogger<ChuteDeletedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(ChuteDeletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理格口删除事件: ChuteId={ChuteId}, ChuteName={ChuteName}, ChuteCode={ChuteCode}",
            notification.ChuteId, notification.ChuteName, notification.ChuteCode);

        await _logRepository.LogInfoAsync(
            $"格口已删除: {notification.ChuteId}",
            $"格口名称: {notification.ChuteName}, 格口编号: {notification.ChuteCode ?? "无"}");
    }
}
