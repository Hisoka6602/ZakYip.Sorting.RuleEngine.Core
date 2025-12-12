using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly IApiCommunicationLogRepository _apiCommunicationLogRepository;

    public WcsApiCalledEventHandler(
        ILogger<WcsApiCalledEventHandler> logger,
        ILogRepository logRepository,
        IApiCommunicationLogRepository apiCommunicationLogRepository)
    {
        _logger = logger;
        _logRepository = logRepository;
        _apiCommunicationLogRepository = apiCommunicationLogRepository;
    }

    public async Task Handle(WcsApiCalledEvent notification, CancellationToken cancellationToken)
    {
        // 持久化API通信日志
        await PersistApiCommunicationLogAsync(notification, cancellationToken).ConfigureAwait(false);
        // 记录日志消息
        if (notification.IsSuccess)
        {
            _logger.LogInformation(
                "处理WCS API调用成功事件: ParcelId={ParcelId}, ApiUrl={ApiUrl}, Duration={DurationMs}ms",
                notification.ParcelId, notification.ApiUrl, notification.DurationMs);

            await _logRepository.LogInfoAsync(
                $"WCS API调用成功: {notification.ParcelId}",
                $"API地址: {notification.ApiUrl}, 状态码: {notification.StatusCode}, 耗时: {notification.DurationMs}ms").ConfigureAwait(false);
        }
        else
        {
            _logger.LogWarning(
                "处理WCS API调用失败事件: ParcelId={ParcelId}, ApiUrl={ApiUrl}, Error={ErrorMessage}",
                notification.ParcelId, notification.ApiUrl, notification.ErrorMessage);

            await _logRepository.LogWarningAsync(
                $"WCS API调用失败: {notification.ParcelId}",
                $"API地址: {notification.ApiUrl}, 错误: {notification.ErrorMessage}, 耗时: {notification.DurationMs}ms").ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 持久化API通信日志到数据库
    /// </summary>
    private async Task PersistApiCommunicationLogAsync(WcsApiCalledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // 如果有完整的API响应数据，使用它；否则从事件中创建基本日志
            var apiLog = notification.ApiResponse != null
                ? MapWcsApiResponseToLog(notification.ApiResponse)
                : CreateBasicLogFromEvent(notification);

            await _apiCommunicationLogRepository.SaveAsync(apiLog, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存API通信日志失败: ParcelId={ParcelId}", notification.ParcelId);
            // 不抛出异常，避免影响主业务流程
        }
    }

    /// <summary>
    /// 将WcsApiResponse映射为ApiCommunicationLog
    /// </summary>
    private static ApiCommunicationLog MapWcsApiResponseToLog(WcsApiResponse response)
    {
        return new ApiCommunicationLog
        {
            ParcelId = response.ParcelId,
            RequestUrl = response.RequestUrl,
            RequestBody = response.RequestBody,
            RequestHeaders = response.RequestHeaders,
            RequestTime = response.RequestTime,
            DurationMs = response.DurationMs,
            ResponseTime = response.ResponseTime,
            ResponseBody = response.ResponseBody,
            ResponseStatusCode = response.ResponseStatusCode,
            ResponseHeaders = response.ResponseHeaders,
            FormattedCurl = response.FormattedCurl,
            IsSuccess = response.Success,
            ErrorMessage = response.ErrorMessage
        };
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
