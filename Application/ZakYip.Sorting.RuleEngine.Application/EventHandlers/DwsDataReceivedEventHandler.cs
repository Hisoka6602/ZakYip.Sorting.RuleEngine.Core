using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// DWS数据接收事件处理器
/// </summary>
public class DwsDataReceivedEventHandler : INotificationHandler<DwsDataReceivedEvent>
{
    private readonly ILogger<DwsDataReceivedEventHandler> _logger;
    private readonly IWcsApiAdapterFactory _apiAdapterFactory;
    private readonly ILogRepository _logRepository;
    private readonly IPublisher _publisher;
    private readonly ISystemClock _clock;

    public DwsDataReceivedEventHandler(
        ILogger<DwsDataReceivedEventHandler> logger,
        IWcsApiAdapterFactory apiAdapterFactory,
        ILogRepository logRepository,
        IPublisher publisher,
        ISystemClock clock)
    {
        _logger = logger;
        _apiAdapterFactory = apiAdapterFactory;
        _logRepository = logRepository;
        _publisher = publisher;
        _clock = clock;
    }

    public async Task Handle(DwsDataReceivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理DWS数据接收事件: ParcelId={ParcelId}, Weight={Weight}g",
            notification.ParcelId, notification.DwsData.Weight);

        await _logRepository.LogInfoAsync(
            $"DWS数据已接收: {notification.ParcelId}",
            $"重量: {notification.DwsData.Weight}g, 体积: {notification.DwsData.Volume}cm³").ConfigureAwait(false);

        // 主动请求格口（主动调用，不发布事件）
        var apiStartTime = _clock.LocalNow;
        try
        {
            var response = await _apiAdapterFactory.GetActiveAdapter().RequestChuteAsync(
                notification.ParcelId,
                notification.DwsData,
                null, // OcrData not available in this event
                cancellationToken).ConfigureAwait(false);

            // 记录WCS API响应（主动调用的响应，直接记录，不通过事件）
            if (response != null)
            {
                var isSuccess = response.RequestStatus == ApiRequestStatus.Success;
                var message = response.FormattedMessage ?? response.ErrorMessage ?? "无消息";
                
                await _logRepository.LogInfoAsync(
                    $"WCS API响应已接收: {notification.ParcelId}",
                    $"成功: {isSuccess}, 消息: {message}").ConfigureAwait(false);
                
                // 发布WCS API调用事件，包含完整的API响应数据
                await _publisher.Publish(new WcsApiCalledEvent
                {
                    ParcelId = notification.ParcelId,
                    ApiUrl = response.RequestUrl ?? "/api/chute/request",
                    IsSuccess = isSuccess,
                    StatusCode = response.ResponseStatusCode,
                    DurationMs = response.DurationMs,
                    CalledAt = _clock.LocalNow,
                    ErrorMessage = isSuccess ? null : (response.ErrorMessage ?? message),
                    ApiResponse = response
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var apiDuration = _clock.LocalNow - apiStartTime;
            
            _logger.LogWarning(ex, "WCS API调用失败，将继续使用规则引擎: ParcelId={ParcelId}", notification.ParcelId);
            await _logRepository.LogWarningAsync(
                $"WCS API调用失败: {notification.ParcelId}",
                ex.Message).ConfigureAwait(false);
            
            // 发布WCS API调用失败事件
            await _publisher.Publish(new WcsApiCalledEvent
            {
                ParcelId = notification.ParcelId,
                ApiUrl = "/api/chute/request",
                IsSuccess = false,
                StatusCode = null,
                DurationMs = (long)apiDuration.TotalMilliseconds,
                CalledAt = _clock.LocalNow,
                ErrorMessage = ex.Message,
                ApiResponse = null
            }, cancellationToken);
        }
    }
}
