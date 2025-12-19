using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 模拟WCS API适配器单元测试
/// Unit tests for MockWcsApiAdapter
/// </summary>
public class MockWcsApiAdapterTests
{
    private readonly Mock<ILogger<MockWcsApiAdapter>> _mockLogger;
    private readonly Mock<IAutoResponseModeService> _mockAutoResponseService;
    private readonly MockWcsApiAdapter _adapter;

    public MockWcsApiAdapterTests()
    {
        _mockLogger = new Mock<ILogger<MockWcsApiAdapter>>();
        _mockAutoResponseService = new Mock<IAutoResponseModeService>();
        _mockAutoResponseService.Setup(s => s.ChuteNumbers).Returns([1, 2, 3]); // Default chute array
        var clock = new MockSystemClock();
        _adapter = new MockWcsApiAdapter(_mockLogger.Object, clock, _mockAutoResponseService.Object);
    }

    [Fact]
    public async Task ScanParcelAsync_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";

        // Act
        var response = await _adapter.ScanParcelAsync(barcode);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ApiRequestStatus.Success, response.RequestStatus);
        Assert.Equal(200, response.ResponseStatusCode.Value);
        Assert.Equal(barcode, response.ParcelId);
        Assert.NotNull(response.ResponseBody);
    }

    [Fact]
    public async Task ScanParcelAsync_ReturnsValidJson()
    {
        // Arrange
        var barcode = "TEST123456";

        // Act
        var response = await _adapter.ScanParcelAsync(barcode);

        // Assert
        var data = JsonSerializer.Deserialize<JsonElement>(response.ResponseBody!);
        Assert.True(data.TryGetProperty("chuteNumber", out var chuteNumberProp));
        var chuteNumber = int.Parse(chuteNumberProp.GetString()!);
        Assert.InRange(chuteNumber, 1, 20);
    }

    [Fact]
    public async Task ScanParcelAsync_UseUtcTime()
    {
        // Arrange
        var barcode = "TEST123456";
        var beforeCall = DateTime.Now;

        // Act
        var response = await _adapter.ScanParcelAsync(barcode);
        var afterCall = DateTime.Now;

        // Assert
        Assert.NotNull(response.RequestTime);
        Assert.NotNull(response.ResponseTime);
        Assert.InRange(response.RequestTime, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
        Assert.InRange(response.ResponseTime!.Value, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }

    [Fact]
    public async Task RequestChuteAsync_ReturnsSuccessResponse()
    {
        // Arrange
        var parcelId = "PKG001";
        var dwsData = new DwsData
        {
            Barcode = "TEST123",
            Weight = 2500.5m,
            Length = 30,
            Width = 20,
            Height = 15,
            Volume = 9000
        };

        // Act
        var response = await _adapter.RequestChuteAsync(parcelId, dwsData);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ApiRequestStatus.Success, response.RequestStatus);
        Assert.Equal(200, response.ResponseStatusCode.Value);
        Assert.Equal(parcelId, response.ParcelId);
        Assert.Equal(200, response.ResponseStatusCode.Value);
    }

    [Fact]
    public async Task RequestChuteAsync_ReturnsRandomChuteInRange()
    {
        // Arrange
        var parcelId = "PKG001";
        var dwsData = new DwsData
        {
            Barcode = "TEST123",
            Weight = 2500.5m,
            Volume = 9000
        };
        var chuteNumbers = new HashSet<int>();

        // Act - Call multiple times to verify randomness
        for (int i = 0; i < 50; i++)
        {
            var response = await _adapter.RequestChuteAsync($"PKG{i:D3}", dwsData);
            var data = JsonSerializer.Deserialize<JsonElement>(response.ResponseBody!);
            var chuteNumber = int.Parse(data.GetProperty("chuteNumber").GetString()!);
            chuteNumbers.Add(chuteNumber);
        }

        // Assert - All chute numbers should be in configured array [1, 2, 3]
        Assert.All(chuteNumbers, cn => Assert.InRange(cn, 1, 3));
        // Should have at least 2 different values in random numbers
        Assert.True(chuteNumbers.Count >= 2);
    }

    [Fact]
    public async Task RequestChuteAsync_IncludesDwsDataInResponse()
    {
        // Arrange
        var parcelId = "PKG001";
        var dwsData = new DwsData
        {
            Barcode = "TEST123",
            Weight = 2500.5m,
            Volume = 9000
        };

        // Act
        var response = await _adapter.RequestChuteAsync(parcelId, dwsData);

        // Assert
        var responseBody = JsonSerializer.Deserialize<JsonElement>(response.ResponseBody!);
        Assert.Equal(dwsData.Weight, responseBody.GetProperty("weight").GetDecimal());
        Assert.Equal(dwsData.Volume, responseBody.GetProperty("volume").GetDecimal());
    }

    [Fact]
    public async Task RequestChuteAsync_IncludesParcelDataInRequestBody()
    {
        // Arrange
        var parcelId = "PKG001";
        var dwsData = new DwsData
        {
            Barcode = "TEST123",
            Weight = 2500.5m,
            Volume = 9000
        };

        // Act
        var response = await _adapter.RequestChuteAsync(parcelId, dwsData);

        // Assert
        var requestBody = JsonSerializer.Deserialize<JsonElement>(response.RequestBody!);
        Assert.Equal(parcelId, requestBody.GetProperty("parcelId").GetString());
        Assert.Equal(dwsData.Barcode, requestBody.GetProperty("barcode").GetString());
    }

    [Fact]
    public async Task UploadImageAsync_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // Mock PNG header

        // Act
        var response = await _adapter.UploadImageAsync(barcode, imageData);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(ApiRequestStatus.Success, response.RequestStatus);
        Assert.Equal(200, response.ResponseStatusCode.Value);
        Assert.Equal(barcode, response.ParcelId);
    }

    [Fact]
    public async Task UploadImageAsync_IncludesImageSizeInResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[1024]; // 1KB image

        // Act
        var response = await _adapter.UploadImageAsync(barcode, imageData);

        // Assert
        var data = JsonSerializer.Deserialize<JsonElement>(response.ResponseBody!);
        Assert.True(data.GetProperty("uploaded").GetBoolean());
        Assert.Equal(imageData.Length, data.GetProperty("size").GetInt32());
    }

    [Fact]
    public async Task UploadImageAsync_LogsInformation()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[512];

        // Act
        await _adapter.UploadImageAsync(barcode, imageData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(barcode)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AllMethods_ReturnConsistentDuration()
    {
        // Arrange
        var barcode = "TEST123";
        var dwsData = new DwsData { Barcode = barcode, Weight = 1000, Volume = 5000 };
        var imageData = new byte[100];

        // Act
        var scanResponse = await _adapter.ScanParcelAsync(barcode);
        var chuteResponse = await _adapter.RequestChuteAsync("PKG001", dwsData);
        var uploadResponse = await _adapter.UploadImageAsync(barcode, imageData);

        // Assert
        Assert.Equal(10, scanResponse.DurationMs);
        Assert.Equal(10, chuteResponse.DurationMs);
        Assert.Equal(10, uploadResponse.DurationMs);
    }

    [Fact]
    public async Task AllMethods_PopulateRequestUrl()
    {
        // Arrange
        var barcode = "TEST123";
        var dwsData = new DwsData { Barcode = barcode, Weight = 1000, Volume = 5000 };
        var imageData = new byte[100];

        // Act
        var scanResponse = await _adapter.ScanParcelAsync(barcode);
        var chuteResponse = await _adapter.RequestChuteAsync("PKG001", dwsData);
        var uploadResponse = await _adapter.UploadImageAsync(barcode, imageData);

        // Assert
        Assert.Equal("/api/mock/scan", scanResponse.RequestUrl);
        Assert.Equal("/api/mock/chute-request", chuteResponse.RequestUrl);
        Assert.Equal("/api/mock/upload-image", uploadResponse.RequestUrl);
    }
}
