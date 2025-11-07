using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 配置缓存失效事件处理器
/// </summary>
public class ConfigurationCacheInvalidatedEventHandler : INotificationHandler<ConfigurationCacheInvalidatedEvent>
{
    private readonly ILogger<ConfigurationCacheInvalidatedEventHandler> _logger;

    public ConfigurationCacheInvalidatedEventHandler(
        ILogger<ConfigurationCacheInvalidatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ConfigurationCacheInvalidatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理配置缓存失效事件: CacheType={CacheType}, Reason={Reason}",
            notification.CacheType, notification.Reason);

        return Task.CompletedTask;
    }
}
