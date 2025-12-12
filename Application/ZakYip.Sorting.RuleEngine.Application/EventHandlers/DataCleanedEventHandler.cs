using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 数据清理事件处理器
/// </summary>
public class DataCleanedEventHandler : INotificationHandler<DataCleanedEvent>
{
    private readonly ILogger<DataCleanedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public DataCleanedEventHandler(
        ILogger<DataCleanedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(DataCleanedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理数据清理事件: TableName={TableName}, RecordCount={RecordCount}, Duration={DurationMs}ms",
            notification.TableName, notification.RecordCount, notification.DurationMs);

        await _logRepository.LogInfoAsync(
            $"数据清理已完成: {notification.TableName}",
            $"清理记录数: {notification.RecordCount}, 截止日期: {notification.CutoffDate:yyyy-MM-dd}, 耗时: {notification.DurationMs}ms").ConfigureAwait(false);
    }
}
