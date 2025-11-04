using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// 第三方API调用事件处理器
/// </summary>
public class ThirdPartyApiCalledEventHandler : INotificationHandler<ThirdPartyApiCalledEvent>
{
    private readonly ILogger<ThirdPartyApiCalledEventHandler> _logger;
    private readonly ILogRepository _logRepository;

    public ThirdPartyApiCalledEventHandler(
        ILogger<ThirdPartyApiCalledEventHandler> logger,
        ILogRepository logRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
    }

    public async Task Handle(ThirdPartyApiCalledEvent notification, CancellationToken cancellationToken)
    {
        if (notification.IsSuccess)
        {
            _logger.LogInformation(
                "处理第三方API调用成功事件: ParcelId={ParcelId}, ApiUrl={ApiUrl}, Duration={DurationMs}ms",
                notification.ParcelId, notification.ApiUrl, notification.DurationMs);

            await _logRepository.LogInfoAsync(
                $"第三方API调用成功: {notification.ParcelId}",
                $"API地址: {notification.ApiUrl}, 状态码: {notification.StatusCode}, 耗时: {notification.DurationMs}ms");
        }
        else
        {
            _logger.LogWarning(
                "处理第三方API调用失败事件: ParcelId={ParcelId}, ApiUrl={ApiUrl}, Error={ErrorMessage}",
                notification.ParcelId, notification.ApiUrl, notification.ErrorMessage);

            await _logRepository.LogWarningAsync(
                $"第三方API调用失败: {notification.ParcelId}",
                $"API地址: {notification.ApiUrl}, 错误: {notification.ErrorMessage}, 耗时: {notification.DurationMs}ms");
        }
    }
}
