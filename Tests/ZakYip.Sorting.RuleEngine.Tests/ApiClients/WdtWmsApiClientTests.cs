using System.Net;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 旺店通WMS API适配器测试
/// WDT WMS API adapter tests
/// </summary>
public class WdtWmsApiClientTests
{
    private readonly Mock<ILogger<WdtWmsApiClient>> _loggerMock;
    private readonly Mock<IWdtWmsConfigRepository> _configRepoMock;
    private readonly MockSystemClock _clock;
    private const string AppKey = "test_app_key";
    private const string AppSecret = "test_app_secret";

    public WdtWmsApiClientTests()
    {
        _loggerMock = new Mock<ILogger<WdtWmsApiClient>>();
        _configRepoMock = new Mock<IWdtWmsConfigRepository>();
        _clock = new MockSystemClock();
        
        // Setup mock config repository to return test configuration
        var testConfig = new WdtWmsConfig
        {
            ConfigId = WdtWmsConfig.SingletonId,
            Url = "https://api.wdt.com",
            Sid = "test_sid",
            AppKey = AppKey,
            AppSecret = AppSecret,
            Method = "wms.logistics.Consign.weigh",
            TimeoutMs = 5000,
            MustIncludeBoxBarcode = false,
            DefaultWeight = 0.0,
            IsEnabled = true,
            Description = "Test configuration",
            CreatedAt = _clock.LocalNow,
            UpdatedAt = _clock.LocalNow
        };
        _configRepoMock.Setup(x => x.GetByIdAsync(WdtWmsConfig.SingletonId))
            .ReturnsAsync(testConfig);
    }

    private WdtWmsApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.wdt.com")
        };
        return new WdtWmsApiClient(httpClient, _loggerMock.Object, _clock, _configRepoMock.Object);
    }

    [Fact]
    public async Task RequestChuteAsync_WithDwsData_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"code\":0,\"message\":\"success\",\"data\":{\"chute\":\"A-101\"}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 1500, Volume = 9000 });

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("查询包裹成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task ScanParcelAsync_NotSupported_ReturnsNotSupportedMessage()
    {
        // Arrange
        var barcode = "TEST123456";

        var client = CreateClient(new Mock<HttpMessageHandler>().Object);

        // Act
        var result = await client.ScanParcelAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("旺店通WMS不支持扫描包裹功能", result.Message);
    }

    [Fact]
    public async Task RequestChuteAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"code\":0,\"message\":\"success\",\"data\":{\"chute\":\"A-101\"}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 1500, Volume = 9000 });

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("查询包裹成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task UploadImageAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        var responseContent = "{\"code\":0,\"message\":\"success\",\"data\":{\"imageId\":\"img_123\"}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.UploadImageAsync(barcode, imageData);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("上传图片成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task RequestChuteAsync_Exception_ReturnsErrorResponse()
    {
        // Arrange
        var barcode = "TEST123456";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 1500, Volume = 9000 });

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Network error", result.Message);
    }
}
