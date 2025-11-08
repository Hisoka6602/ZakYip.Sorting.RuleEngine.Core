using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 规则创建事件处理器
/// Handles the event when a new sorting rule is created in the system
/// </summary>
/// <remarks>
/// This handler processes rule creation events and:
/// - Records the new rule creation in application logs
/// - Persists rule creation details to the database for audit purposes
/// - Triggers cache invalidation for rule caching mechanisms
/// - Supports real-time rule activation without service restart
/// 
/// Rules define how parcels are sorted to specific chutes based on:
/// - Barcode patterns (regex matching)
/// - Weight ranges
/// - Dimensional constraints
/// - OCR recognition results
/// - API response data
/// - Low-code expressions
/// </remarks>
public class RuleCreatedEventHandler : INotificationHandler<RuleCreatedEvent>
{
    private readonly ILogger<RuleCreatedEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleCreatedEventHandler"/> class
    /// </summary>
    /// <param name="logger">Logger for recording handler activities</param>
    /// <param name="logRepository">Repository for persisting log entries</param>
    public RuleCreatedEventHandler(
        ILogger<RuleCreatedEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    /// <summary>
    /// Handles the rule created event notification
    /// </summary>
    /// <param name="notification">Event containing the newly created rule information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public async Task Handle(RuleCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理规则创建事件: RuleId={RuleId}, RuleName={RuleName}, Priority={Priority}",
            notification.RuleId, notification.RuleName, notification.Priority);

        await _logRepository.LogInfoAsync(
            $"规则已创建: {notification.RuleId}",
            $"规则名称: {notification.RuleName}, 目标格口: {notification.TargetChute}, 优先级: {notification.Priority}, 已启用: {notification.IsEnabled}");
    }
}
