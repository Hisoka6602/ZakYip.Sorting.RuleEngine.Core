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
    private readonly IThirdPartyApiAdapterFactory _apiAdapterFactory;
    private readonly ILogRepository _logRepository;
    private readonly IPublisher _publisher;

    public DwsDataReceivedEventHandler(
        ILogger<DwsDataReceivedEventHandler> logger,
        IThirdPartyApiAdapterFactory apiAdapterFactory,
        ILogRepository logRepository,
        IPublisher publisher)
    {
        _logger = logger;
        _apiAdapterFactory = apiAdapterFactory;
        _logRepository = logRepository;
        _publisher = publisher;
    }

    public async Task Handle(DwsDataReceivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理DWS数据接收事件: ParcelId={ParcelId}, Weight={Weight}g",
            notification.ParcelId, notification.DwsData.Weight);

        await _logRepository.LogInfoAsync(
            $"DWS数据已接收: {notification.ParcelId}",
            $"重量: {notification.DwsData.Weight}g, 体积: {notification.DwsData.Volume}cm³");

        // 主动上传DWS数据到第三方API（主动调用，不发布事件）
        var apiStartTime = DateTime.UtcNow;
        try
        {
            var parcelInfo = new Domain.Entities.ParcelInfo
            {
                ParcelId = notification.ParcelId,
                Barcode = notification.DwsData.Barcode,
                Status = ParcelStatus.Processing
            };

            var response = await _apiAdapterFactory.GetActiveAdapter().UploadDataAsync(
                parcelInfo,
                notification.DwsData,
                cancellationToken);

            var apiDuration = DateTime.UtcNow - apiStartTime;

            // 记录第三方API响应（主动调用的响应，直接记录，不通过事件）
            if (response != null)
            {
                await _logRepository.LogInfoAsync(
                    $"第三方API响应已接收: {notification.ParcelId}",
                    $"成功: {response.Success}, 消息: {response.Message}");
                
                // 发布第三方API调用事件
                await _publisher.Publish(new ThirdPartyApiCalledEvent
                {
                    ParcelId = notification.ParcelId,
                    ApiUrl = "/api/sorting/upload",
                    IsSuccess = response.Success,
                    StatusCode = int.TryParse(response.Code, out var code) ? code : null,
                    DurationMs = (long)apiDuration.TotalMilliseconds,
                    CalledAt = DateTime.Now,
                    ErrorMessage = response.Success ? null : response.Message
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var apiDuration = DateTime.UtcNow - apiStartTime;
            
            _logger.LogWarning(ex, "第三方API调用失败，将继续使用规则引擎: ParcelId={ParcelId}", notification.ParcelId);
            await _logRepository.LogWarningAsync(
                $"第三方API调用失败: {notification.ParcelId}",
                ex.Message);
            
            // 发布第三方API调用失败事件
            await _publisher.Publish(new ThirdPartyApiCalledEvent
            {
                ParcelId = notification.ParcelId,
                ApiUrl = "/api/sorting/upload",
                IsSuccess = false,
                StatusCode = null,
                DurationMs = (long)apiDuration.TotalMilliseconds,
                CalledAt = DateTime.Now,
                ErrorMessage = ex.Message
            }, cancellationToken);
        }
    }
}
