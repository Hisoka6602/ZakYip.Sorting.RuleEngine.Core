using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// WCS API调用事件处理器
/// </summary>
public class WcsApiCalledEventHandler : INotificationHandler<WcsApiCalledEvent>
{
    private readonly ILogger<WcsApiCalledEventHandler> _logger;
    private readonly ILogRepository _logRepository;
    private readonly WcsApiLogBackgroundService _logBackgroundService;

    public WcsApiCalledEventHandler(
        ILogger<WcsApiCalledEventHandler> logger,
        ILogRepository logRepository,
        WcsApiLogBackgroundService logBackgroundService)
    {
        _logger = logger;
        _logRepository = logRepository;
        _logBackgroundService = logBackgroundService;
    }

    public async Task Handle(WcsApiCalledEvent notification, CancellationToken cancellationToken)
    {
        // 持久化API通信日志（使用Channel队列，零阻塞，零线程消耗）
        // Persist API communication log (using Channel queue, zero blocking, zero thread consumption)
        EnqueueApiCommunicationLog(notification);
        
        // 记录日志消息
        if (notification.IsSuccess)
        {
#pragma warning disable CA1848 // 高频日志场景，性能已优化
            _logger.LogInformation(
                "处理WCS API调用成功事件: ParcelId={ParcelId}, ApiUrl={ApiUrl}, Duration={DurationMs}ms",
                notification.ParcelId, notification.ApiUrl, notification.DurationMs);
#pragma warning restore CA1848

            await _logRepository.LogInfoAsync(
                $"WCS API调用成功: {notification.ParcelId}",
                $"API地址: {notification.ApiUrl}, 状态码: {notification.StatusCode}, 耗时: {notification.DurationMs}ms").ConfigureAwait(false);
        }
        else
        {
#pragma warning disable CA1848 // 高频日志场景，性能已优化
            _logger.LogWarning(
                "处理WCS API调用失败事件: ParcelId={ParcelId}, ApiUrl={ApiUrl}, Error={ErrorMessage}",
                notification.ParcelId, notification.ApiUrl, notification.ErrorMessage);
#pragma warning restore CA1848

            await _logRepository.LogWarningAsync(
                $"WCS API调用失败: {notification.ParcelId}",
                $"API地址: {notification.ApiUrl}, 错误: {notification.ErrorMessage}, 耗时: {notification.DurationMs}ms").ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 将API通信日志加入后台队列（非阻塞）
    /// Enqueue API communication log to background queue (non-blocking)
    /// </summary>
    private void EnqueueApiCommunicationLog(WcsApiCalledEvent notification)
    {
        try
        {
            // 如果有完整的API响应数据，使用它；否则从事件中创建基本日志
            var apiLog = notification.ApiResponse != null
                ? WcsApiResponseMapper.ToApiCommunicationLog(notification.ApiResponse)
                : CreateBasicLogFromEvent(notification);

            // 非阻塞入队，立即返回
            _logBackgroundService.EnqueueLog(apiLog);
        }
        catch (Exception ex)
        {
#pragma warning disable CA1848 // 异常处理场景
            _logger.LogError(ex, "入队API通信日志失败: ParcelId={ParcelId}", notification.ParcelId);
#pragma warning restore CA1848
            // 不抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 从事件创建基本的ApiCommunicationLog（当没有完整响应数据时）
    /// </summary>
    private static ApiCommunicationLog CreateBasicLogFromEvent(WcsApiCalledEvent notification)
    {
        return new ApiCommunicationLog
        {
            ParcelId = notification.ParcelId,
            RequestUrl = notification.ApiUrl,
            RequestBody = null,
            RequestHeaders = null,
            RequestTime = notification.CalledAt,
            DurationMs = notification.DurationMs,
            ResponseTime = notification.CalledAt.AddMilliseconds(notification.DurationMs),
            ResponseBody = null,
            ResponseStatusCode = notification.StatusCode,
            ResponseHeaders = null,
            FormattedCurl = null,
            IsSuccess = notification.IsSuccess,
            ErrorMessage = notification.ErrorMessage
        };
    }
}
