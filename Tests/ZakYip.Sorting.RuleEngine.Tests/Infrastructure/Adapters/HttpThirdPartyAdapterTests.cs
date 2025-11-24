using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.ThirdParty;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Adapters;

/// <summary>
/// HTTP第三方适配器测试
/// HTTP third party adapter tests
/// </summary>
public class HttpThirdPartyAdapterTests
{
    private readonly Mock<ILogger<HttpThirdPartyAdapter>> _mockLogger;

    public HttpThirdPartyAdapterTests()
    {
        _mockLogger = new Mock<ILogger<HttpThirdPartyAdapter>>();
    }

    [Fact]
    public void AdapterName_ShouldReturnCorrectName()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var adapter = new HttpThirdPartyAdapter(
            httpClient,
            "https://api.example.com/endpoint",
            _mockLogger.Object);

        // Act & Assert
        Assert.Equal("HTTP-Generic", adapter.AdapterName);
    }

    [Fact]
    public void ProtocolType_ShouldReturnHTTP()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var adapter = new HttpThirdPartyAdapter(
            httpClient,
            "https://api.example.com/endpoint",
            _mockLogger.Object);

        // Act & Assert
        Assert.Equal("HTTP", adapter.ProtocolType);
    }

    [Fact]
    public async Task CallApiAsync_WithSuccessResponse_ShouldReturnSuccessResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"result\":\"success\"}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var adapter = new HttpThirdPartyAdapter(
            httpClient,
            "https://api.example.com/endpoint",
            _mockLogger.Object);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG-001", CartNumber = "CART-01" };
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1250.5m,
            Length = 30,
            Width = 20,
            Height = 10,
            Volume = 6000
        };

        // Act
        var result = await adapter.CallApiAsync(parcelInfo, dwsData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
    }

    [Fact]
    public async Task CallApiAsync_WithErrorResponse_ShouldReturnFailureResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"Invalid request\"}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var adapter = new HttpThirdPartyAdapter(
            httpClient,
            "https://api.example.com/endpoint",
            _mockLogger.Object);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG-002", CartNumber = "CART-02" };
        var dwsData = new DwsData
        {
            Barcode = "0987654321",
            Weight = 2000.0m,
            Length = 40,
            Width = 30,
            Height = 20,
            Volume = 24000
        };

        // Act
        var result = await adapter.CallApiAsync(parcelInfo, dwsData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("400", result.Code);
    }

    [Fact]
    public async Task CallApiAsync_WithHttpException_ShouldReturnErrorResult()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var adapter = new HttpThirdPartyAdapter(
            httpClient,
            "https://api.example.com/endpoint",
            _mockLogger.Object);

        var parcelInfo = new ParcelInfo { ParcelId = "PKG-003", CartNumber = "CART-03" };
        var dwsData = new DwsData
        {
            Barcode = "1111111111",
            Weight = 500.0m,
            Length = 10,
            Width = 10,
            Height = 10,
            Volume = 1000
        };

        // Act
        var result = await adapter.CallApiAsync(parcelInfo, dwsData);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
    }

    [Fact]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var adapter = new HttpThirdPartyAdapter(
            httpClient,
            "https://test-api.example.com/v1/endpoint",
            _mockLogger.Object);

        // Assert
        Assert.Equal("HTTP-Generic", adapter.AdapterName);
        Assert.Equal("HTTP", adapter.ProtocolType);
    }
}
