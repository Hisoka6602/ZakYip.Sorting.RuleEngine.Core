using MediatR;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.EventHandlers;

/// <summary>
/// DWS数据接收事件处理器
/// Handler for DWS data received events - uploads data to third-party API
/// </summary>
public class DwsDataReceivedEventHandler : INotificationHandler<DwsDataReceivedEvent>
{
    private readonly ILogger<DwsDataReceivedEventHandler> _logger;
    private readonly IThirdPartyApiClient _thirdPartyApiClient;
    private readonly ILogRepository _logRepository;
    private readonly IPublisher _publisher;

    public DwsDataReceivedEventHandler(
        ILogger<DwsDataReceivedEventHandler> logger,
        IThirdPartyApiClient thirdPartyApiClient,
        ILogRepository logRepository,
        IPublisher publisher)
    {
        _logger = logger;
        _thirdPartyApiClient = thirdPartyApiClient;
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

        // 上传DWS数据到第三方API
        try
        {
            var parcelInfo = new Domain.Entities.ParcelInfo
            {
                ParcelId = notification.ParcelId,
                Barcode = notification.DwsData.Barcode,
                Status = ParcelStatus.Processing
            };

            var response = await _thirdPartyApiClient.UploadDataAsync(
                parcelInfo,
                notification.DwsData,
                cancellationToken);

            // 发布第三方响应接收事件
            if (response != null)
            {
                await _publisher.Publish(new ThirdPartyResponseReceivedEvent
                {
                    ParcelId = notification.ParcelId,
                    Response = response
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "第三方API调用失败，将继续使用规则引擎: ParcelId={ParcelId}", notification.ParcelId);
            await _logRepository.LogWarningAsync(
                $"第三方API调用失败: {notification.ParcelId}",
                ex.Message);
        }
    }
}
