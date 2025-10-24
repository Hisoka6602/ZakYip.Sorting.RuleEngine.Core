using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 包裹创建事件处理器
/// Handler for parcel creation events - creates parcel space in cache
/// </summary>
public class ParcelCreatedEventHandler : INotificationHandler<ParcelCreatedEvent>
{
    private readonly ILogger<ParcelCreatedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public ParcelCreatedEventHandler(
        ILogger<ParcelCreatedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(ParcelCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理包裹创建事件: ParcelId={ParcelId}, CartNumber={CartNumber}, Sequence={Sequence}",
            notification.ParcelId, notification.CartNumber, notification.SequenceNumber);

        // 记录包裹创建事件到数据库
        await _logRepository.LogInfoAsync(
            $"包裹已创建: {notification.ParcelId}",
            $"小车号: {notification.CartNumber}, 序号: {notification.SequenceNumber}");

        // 此处可以开辟缓存空间等待DWS数据
        // Space can be allocated in cache to wait for DWS data
    }
}
