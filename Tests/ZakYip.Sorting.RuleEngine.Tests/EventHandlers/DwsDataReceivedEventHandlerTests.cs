using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using ZakYip.Sorting.RuleEngine.Application.EventHandlers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

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
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly DwsDataReceivedEventHandler _handler;

    public DwsDataReceivedEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<DwsDataReceivedEventHandler>>();
        _mockFactory = new Mock<IWcsApiAdapterFactory>();
        _mockAdapter = new Mock<IWcsApiAdapter>();
        _mockFactory.Setup(f => f.GetActiveAdapter()).Returns(_mockAdapter.Object);
        _mockLogRepository = new Mock<ILogRepository>();
        _mockPublisher = new Mock<IPublisher>();

        _handler = new DwsDataReceivedEventHandler(
            _mockLogger.Object,
            _mockFactory.Object,
            _mockLogRepository.Object,
            _mockPublisher.Object);
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
            Success = true,
            Message = "Upload successful"
        };

        _mockAdapter.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockAdapter.Verify(
            a => a.UploadDataAsync(
                It.Is<ParcelInfo>(p => p.ParcelId == "PKG001"),
                It.Is<DwsData>(d => d.Weight == 1500),
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

        var apiResponse = new WcsApiResponse { Success = true };
        _mockAdapter.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
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
            Success = true,
            Code = "200",
            Message = "Success"
        };

        _mockAdapter.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
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
            Success = false,
            Code = "500",
            Message = "Server error"
        };

        _mockAdapter.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
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

        _mockAdapter.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
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

        _mockAdapter.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WcsApiResponse?)null);

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

        var apiResponse = new WcsApiResponse { Success = true };
        _mockAdapter.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
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
