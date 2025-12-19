using ZakYip.Sorting.RuleEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using ZakYip.Sorting.RuleEngine.Application.DTOs;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// 包裹处理服务单元测试
/// Unit tests for ParcelProcessingService
/// </summary>
public class ParcelProcessingServiceTests
{
    private readonly Mock<IRuleEngineService> _mockRuleEngineService;
    private readonly Mock<IWcsApiAdapter> _mockApiAdapter;
    private readonly Mock<IWcsApiAdapterFactory> _mockFactory;
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<ILogger<ParcelProcessingService>> _mockLogger;
    private readonly Mock<ISystemClock> _mockClock;
    private readonly ParcelProcessingService _service;

    public ParcelProcessingServiceTests()
    {
        _mockRuleEngineService = new Mock<IRuleEngineService>();
        _mockApiAdapter = new Mock<IWcsApiAdapter>();
        _mockFactory = new Mock<IWcsApiAdapterFactory>();
        _mockFactory.Setup(f => f.GetActiveAdapter()).Returns(_mockApiAdapter.Object);
        _mockLogRepository = new Mock<ILogRepository>();
        _mockLogger = new Mock<ILogger<ParcelProcessingService>>();
        _mockClock = new Mock<ISystemClock>();
        var fixedTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
        _mockClock.SetupGet(c => c.LocalNow).Returns(fixedTime);

        _service = new ParcelProcessingService(
            _mockRuleEngineService.Object,
            _mockFactory.Object,
            _mockLogRepository.Object,
            _mockLogger.Object,
            _mockClock.Object);
    }

    [Fact]
    public async Task ProcessParcelAsync_Should_ThrowArgumentNullException_When_RequestIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.ProcessParcelAsync(null!));
    }

    [Fact]
    public async Task ProcessParcelAsync_ValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Barcode = "1234567890",
            Weight = 1500,
            Volume = 9000
        };

        var expectedChute = "CHUTE-A01";
        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChute);

        // Act
        var response = await _service.ProcessParcelAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal("PKG001", response.ParcelId);
        Assert.Equal(expectedChute, response.ChuteNumber);
        Assert.Null(response.ErrorMessage);
        Assert.True(response.ProcessingTimeMs >= 0);
    }

    [Fact]
    public async Task ProcessParcelAsync_RuleEngineReturnsNull_ReturnsFailureResponse()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG002",
            CartNumber = "CART002",
            Barcode = "9876543210"
        };

        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var response = await _service.ProcessParcelAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.Equal("PKG002", response.ParcelId);
        Assert.Null(response.ChuteNumber);
    }

    [Fact]
    public async Task ProcessParcelAsync_WithBarcode_CallsWcsApi()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG003",
            CartNumber = "CART003",
            Barcode = "1234567890",
            Weight = 2000,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var apiResponse = new WcsApiResponse
        {
            ResponseStatusCode = 200,
            FormattedMessage = "Success",
            ResponseBody = "Test Data"
        };

        _mockApiAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResponse);

        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("CHUTE-B01");

        // Act
        var response = await _service.ProcessParcelAsync(request);

        // Assert
        _mockApiAdapter.Verify(
            a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ProcessParcelAsync_ThirdPartyApiThrows_ContinuesWithRuleEngine()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG004",
            CartNumber = "CART004",
            Barcode = "1234567890",
            Weight = 1000,
            Volume = 5000
        };

        _mockApiAdapter.Setup(a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API connection failed"));

        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("CHUTE-C01");

        // Act
        var response = await _service.ProcessParcelAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal("CHUTE-C01", response.ChuteNumber);
        _mockLogRepository.Verify(
            l => l.LogWarningAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessParcelAsync_RuleEngineThrows_ReturnsFailureResponse()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG005",
            CartNumber = "CART005"
        };

        var exceptionMessage = "Rule engine error";
        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var response = await _service.ProcessParcelAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.Equal("PKG005", response.ParcelId);
        Assert.Equal(exceptionMessage, response.ErrorMessage);
        _mockLogRepository.Verify(
            l => l.LogErrorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessParcelAsync_WithoutBarcode_SkipsWcsApi()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG006",
            CartNumber = "CART006"
            // No Barcode
        };

        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("CHUTE-D01");

        // Act
        var response = await _service.ProcessParcelAsync(request);

        // Assert
        _mockApiAdapter.Verify(
            a => a.RequestChuteAsync(
                It.IsAny<string>(), It.IsAny<DwsData>(), It.IsAny<OcrData?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ProcessParcelAsync_SuccessfulProcessing_LogsInfoMessage()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG007",
            CartNumber = "CART007"
        };

        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("CHUTE-E01");

        // Act
        await _service.ProcessParcelAsync(request);

        // Assert
        _mockLogRepository.Verify(
            l => l.LogInfoAsync(
                It.Is<string>(s => s.Contains("PKG007")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessParcelsAsync_MultipleParcels_ProcessesAll()
    {
        // Arrange
        var requests = new List<ParcelProcessRequest>
        {
            new ParcelProcessRequest { ParcelId = "PKG101", CartNumber = "CART101" },
            new ParcelProcessRequest { ParcelId = "PKG102", CartNumber = "CART102" },
            new ParcelProcessRequest { ParcelId = "PKG103", CartNumber = "CART103" }
        };

        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("CHUTE-F01");

        // Act
        var responses = await _service.ProcessParcelsAsync(requests);

        // Assert
        Assert.Equal(3, responses.Count());
        Assert.All(responses, r => Assert.True(r.Success));
        Assert.Contains(responses, r => r.ParcelId == "PKG101");
        Assert.Contains(responses, r => r.ParcelId == "PKG102");
        Assert.Contains(responses, r => r.ParcelId == "PKG103");
    }

    [Fact]
    public async Task ProcessParcelsAsync_SomeParcelsFail_ReturnsAllResponses()
    {
        // Arrange
        var requests = new List<ParcelProcessRequest>
        {
            new ParcelProcessRequest { ParcelId = "PKG201", CartNumber = "CART201" },
            new ParcelProcessRequest { ParcelId = "PKG202", CartNumber = "CART202" }
        };

        _mockRuleEngineService.SetupSequence(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("CHUTE-G01")
            .ThrowsAsync(new Exception("Processing error"));

        // Act
        var responses = await _service.ProcessParcelsAsync(requests);

        // Assert
        Assert.Equal(2, responses.Count());
        Assert.Single(responses, r => r.Success);
        Assert.Single(responses, r => !r.Success);
    }

    [Fact]
    public async Task ProcessParcelsAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var requests = new List<ParcelProcessRequest>();

        // Act
        var responses = await _service.ProcessParcelsAsync(requests);

        // Assert
        Assert.Empty(responses);
    }

    [Fact]
    public async Task ProcessParcelAsync_RecordsProcessingTime_ReturnsNonZeroTime()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG008",
            CartNumber = "CART008"
        };

        _mockRuleEngineService.Setup(r => r.EvaluateRulesAsync(
                It.IsAny<ParcelInfo>(),
                It.IsAny<DwsData>(),
                It.IsAny<WcsApiResponse>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("CHUTE-H01");

        // Act
        var response = await _service.ProcessParcelAsync(request);

        // Assert
        Assert.True(response.ProcessingTimeMs >= 0);
    }
}
