using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 数据归档事件处理器
/// </summary>
public class DataArchivedEventHandler : INotificationHandler<DataArchivedEvent>
{
    private readonly ILogger<DataArchivedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public DataArchivedEventHandler(
        ILogger<DataArchivedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(DataArchivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理数据归档事件: RecordCount={RecordCount}, Duration={DurationMs}ms",
            notification.RecordCount, notification.DurationMs);

        await _logRepository.LogInfoAsync(
            $"数据归档已完成",
            $"归档记录数: {notification.RecordCount}, 时间范围: {notification.StartDate:yyyy-MM-dd} - {notification.EndDate:yyyy-MM-dd}, 耗时: {notification.DurationMs}ms");
    }
}
