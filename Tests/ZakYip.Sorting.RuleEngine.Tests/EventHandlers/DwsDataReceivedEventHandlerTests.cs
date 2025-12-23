using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ZakYip.Sorting.RuleEngine.Application.EventHandlers;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.EventHandlers;

/// <summary>
/// DWS数据接收事件处理器单元测试
/// Unit tests for DwsDataReceivedEventHandler
/// </summary>
public class DwsDataReceivedEventHandlerTests
{
    private readonly Mock<ILogger<DwsDataReceivedEventHandler>> _mockLogger;
    private readonly Mock<IWcsApiAdapterFactory> _mockFactory;
    private readonly Mock<IWcsApiAdapter> _mockAdapter;
    private readonly Mock<IDownstreamCommunication> _mockSorterManager;
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly Mock<IParcelInfoRepository> _mockParcelRepository;
    private readonly Mock<IParcelLifecycleNodeRepository> _mockLifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly DwsDataReceivedEventHandler _handler;

    public DwsDataReceivedEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<DwsDataReceivedEventHandler>>();
        _mockFactory = new Mock<IWcsApiAdapterFactory>();
        _mockAdapter = new Mock<IWcsApiAdapter>();
        _mockFactory.Setup(f => f.GetActiveAdapter()).Returns(_mockAdapter.Object);
        _mockSorterManager = new Mock<IDownstreamCommunication>();
        _mockLogRepository = new Mock<ILogRepository>();
        _mockPublisher = new Mock<IPublisher>();
        _mockParcelRepository = new Mock<IParcelInfoRepository>();
        _mockLifecycleRepository = new Mock<IParcelLifecycleNodeRepository>();
        
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheLogger = new Mock<ILogger<ParcelCacheService>>();
        _cacheService = new ParcelCacheService(memoryCache, cacheLogger.Object);
        var clock = new MockSystemClock();

        _handler = new DwsDataReceivedEventHandler(
            _mockLogger.Object,
            _mockFactory.Object,
            _mockSorterManager.Object,
            _mockLogRepository.Object,
            _mockPublisher.Object,
            clock,
            _mockParcelRepository.Object,
            _mockLifecycleRepository.Object,
            _cacheService);
            
        // Setup默认返回值
        _mockParcelRepository.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParcelInfo?)null);
        _mockParcelRepository.Setup(x => x.GetLatestWithoutDwsDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParcelInfo { ParcelId = "TEST001", CartNumber = "CART001" });
        _mockParcelRepository.Setup(x => x.UpdateAsync(It.IsAny<ParcelInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockLifecycleRepository.Setup(x => x.AddAsync(It.IsAny<ParcelLifecycleNodeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_ValidEvent_CallsWcsApi()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG001", 
            DwsData = dwsData 
        };

        var apiResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            FormattedMessage = "Upload successful"
        };

        _mockAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockAdapter.Verify(
            a => a.RequestChuteAsync(
                It.Is<string>(b => b == notification.ParcelId), It.Is<DwsData>(d => d.Barcode == notification.DwsData.Barcode), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EventWithDwsData_LogsInfo()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 2000,
            Length = 350,
            Width = 250,
            Height = 200,
            Volume = 17500
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG002", 
            DwsData = dwsData 
        };

        var apiResponse = new WcsApiResponse { RequestStatus = ApiRequestStatus.Success };
        _mockAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        _mockLogRepository.Verify(
            l => l.LogInfoAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WcsApiSuccess_PublishesSuccessEvent()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG003", 
            DwsData = dwsData 
        };

        var apiResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            ResponseStatusCode = 200,
            FormattedMessage = "Success"
        };

        _mockAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockPublisher.Verify(
            p => p.Publish(
                It.Is<WcsApiCalledEvent>(e => e.IsSuccess && e.ParcelId == "PKG003"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WcsApiFailure_PublishesFailureEvent()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG004", 
            DwsData = dwsData 
        };

        var apiResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Failure,
            ResponseStatusCode = 500,
            FormattedMessage = "Server error"
        };

        _mockAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockPublisher.Verify(
            p => p.Publish(
                It.Is<WcsApiCalledEvent>(e => !e.IsSuccess && e.ParcelId == "PKG004"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WcsApiThrowsException_PublishesFailureEvent()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG005", 
            DwsData = dwsData 
        };

        _mockAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockPublisher.Verify(
            p => p.Publish(
                It.Is<WcsApiCalledEvent>(e => !e.IsSuccess && e.ErrorMessage != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WcsApiReturnsNull_DoesNotPublishEvent()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG006", 
            DwsData = dwsData 
        };

        _mockAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WcsApiResponse)null!);  // 测试null场景

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockPublisher.Verify(
            p => p.Publish(
                It.IsAny<WcsApiCalledEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidEvent_LogsDwsDataDetails()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 3000,
            Length = 400,
            Width = 300,
            Height = 250,
            Volume = 30000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG007", 
            DwsData = dwsData 
        };

        var apiResponse = new WcsApiResponse { RequestStatus = ApiRequestStatus.Success };
        _mockAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            l => l.LogInfoAsync(
                It.Is<string>(s => s.Contains("PKG007")),
                It.Is<string>(s => s.Contains("3000")),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
