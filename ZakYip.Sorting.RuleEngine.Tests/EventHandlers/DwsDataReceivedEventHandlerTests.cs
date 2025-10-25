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
    private readonly Mock<IThirdPartyApiClient> _mockApiClient;
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly DwsDataReceivedEventHandler _handler;

    public DwsDataReceivedEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<DwsDataReceivedEventHandler>>();
        _mockApiClient = new Mock<IThirdPartyApiClient>();
        _mockLogRepository = new Mock<ILogRepository>();
        _mockPublisher = new Mock<IPublisher>();

        _handler = new DwsDataReceivedEventHandler(
            _mockLogger.Object,
            _mockApiClient.Object,
            _mockLogRepository.Object,
            _mockPublisher.Object);
    }

    [Fact]
    public async Task Handle_ValidEvent_CallsThirdPartyApi()
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

        var apiResponse = new ThirdPartyResponse
        {
            Success = true,
            Message = "Upload successful"
        };

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockApiClient.Verify(
            a => a.UploadDataAsync(
                It.Is<ParcelInfo>(p => p.ParcelId == "PKG001"),
                It.Is<DwsData>(d => d.Weight == 1500),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidEvent_LogsInfoMessage()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG002", 
            DwsData = dwsData 
        };

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyResponse { Success = true });

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            l => l.LogInfoAsync(
                It.Is<string>(s => s.Contains("PKG002")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ApiSuccess_LogsResponseMessage()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG003", 
            DwsData = dwsData 
        };

        var apiResponse = new ThirdPartyResponse
        {
            Success = true,
            Message = "Data uploaded successfully"
        };

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            l => l.LogInfoAsync(
                It.Is<string>(s => s.Contains("第三方API响应已接收")),
                It.Is<string>(s => s.Contains("Data uploaded successfully")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ApiThrowsException_LogsWarning()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG004", 
            DwsData = dwsData 
        };

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API connection failed"));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            l => l.LogWarningAsync(
                It.Is<string>(s => s.Contains("第三方API调用失败")),
                It.Is<string>(s => s.Contains("API connection failed")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ApiThrowsException_DoesNotThrow()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG005", 
            DwsData = dwsData 
        };

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API error"));

        // Act & Assert - Should not throw
        await _handler.Handle(notification, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ApiReturnsNull_DoesNotLogResponse()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG006", 
            DwsData = dwsData 
        };

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThirdPartyResponse?)null);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            l => l.LogInfoAsync(
                It.Is<string>(s => s.Contains("第三方API响应已接收")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_PassesToApiClient()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Volume = 9000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG007", 
            DwsData = dwsData 
        };
        var cts = new CancellationTokenSource();

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyResponse { Success = true });

        // Act
        await _handler.Handle(notification, cts.Token);

        // Assert
        _mockApiClient.Verify(
            a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.Is<CancellationToken>(ct => ct == cts.Token)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsInitialDwsDataReceived_BeforeApiCall()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 2500,
            Volume = 15000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG008", 
            DwsData = dwsData 
        };

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyResponse { Success = true });

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            l => l.LogInfoAsync(
                It.Is<string>(s => s.Contains("DWS数据已接收")),
                It.Is<string>(s => s.Contains("2500") && s.Contains("15000")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PopulatesParcelInfoCorrectly()
    {
        // Arrange
        var dwsData = new DwsData
        {
            Barcode = "TEST123456",
            Weight = 1000,
            Volume = 5000
        };

        var notification = new DwsDataReceivedEvent 
        { 
            ParcelId = "PKG009", 
            DwsData = dwsData 
        };

        _mockApiClient.Setup(a => a.UploadDataAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyResponse { Success = true });

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        _mockApiClient.Verify(
            a => a.UploadDataAsync(
                It.Is<ParcelInfo>(p => 
                    p.ParcelId == "PKG009" && 
                    p.Barcode == "TEST123456"),
                It.IsAny<DwsData>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
