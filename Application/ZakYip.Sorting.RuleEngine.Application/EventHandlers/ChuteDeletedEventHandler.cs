using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 格口删除事件处理器
/// Handles the event when a chute is deleted from the system
/// </summary>
/// <remarks>
/// This handler performs the following operations:
/// - Logs the chute deletion event to the application logs
/// - Records the deletion details in the database for audit purposes
/// - Supports distributed event handling via MediatR notifications
/// </remarks>
public class ChuteDeletedEventHandler : INotificationHandler<ChuteDeletedEvent>
{
    private readonly ILogger<ChuteDeletedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChuteDeletedEventHandler"/> class
    /// </summary>
    /// <param name="logger">Logger instance for recording handler activities</param>
    /// <param name="logRepository">Repository for persisting log entries</param>
    public ChuteDeletedEventHandler(
        ILogger<ChuteDeletedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    /// <summary>
    /// Handles the chute deleted event notification
    /// </summary>
    /// <param name="notification">The event notification containing chute deletion details</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
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
