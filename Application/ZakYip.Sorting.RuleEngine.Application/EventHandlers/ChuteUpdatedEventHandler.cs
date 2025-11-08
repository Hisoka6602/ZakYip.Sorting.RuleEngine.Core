using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 格口更新事件处理器
/// Handles the event when a chute is updated in the system
/// </summary>
/// <remarks>
/// This handler processes chute update events and performs:
/// - Logging the update to application logs for audit trail
/// - Recording configuration changes in the database
/// - Supporting distributed event handling via MediatR
/// 
/// Chute updates include changes to:
/// - Chute name and code
/// - Enabled/disabled status
/// - Description and configuration
/// </remarks>
public class ChuteUpdatedEventHandler : INotificationHandler<ChuteUpdatedEvent>
{
    private readonly ILogger<ChuteUpdatedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChuteUpdatedEventHandler"/> class
    /// </summary>
    /// <param name="logger">Logger for recording handler activities</param>
    /// <param name="logRepository">Repository for persisting log entries</param>
    public ChuteUpdatedEventHandler(
        ILogger<ChuteUpdatedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    /// <summary>
    /// Handles the chute updated event notification
    /// </summary>
    /// <param name="notification">Event containing updated chute information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
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
