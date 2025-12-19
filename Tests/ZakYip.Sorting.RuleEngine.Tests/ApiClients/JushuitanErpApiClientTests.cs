using ZakYip.Sorting.RuleEngine.Domain.Enums;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 聚水潭ERP API适配器测试
/// Jushuituan ERP API adapter tests
/// </summary>
public class JushuitanErpApiClientTests
{
    private readonly Mock<ILogger<JushuitanErpApiClient>> _loggerMock;
    private readonly Mock<IJushuitanErpConfigRepository> _configRepoMock;
    private readonly MockSystemClock _clock;
    private const string PartnerKey = "test_partner_key";
    private const string PartnerSecret = "test_partner_secret";
    private const string Token = "test_token";

    public JushuitanErpApiClientTests()
    {
        _loggerMock = new Mock<ILogger<JushuitanErpApiClient>>();
        _configRepoMock = new Mock<IJushuitanErpConfigRepository>();
        _clock = new MockSystemClock();
        
        // Setup mock config repository to return test configuration
        var testConfig = new JushuitanErpConfig
        {
            ConfigId = JushuitanErpConfig.SingletonId,
            Url = "https://api.jushuitan.com",
            AppKey = PartnerKey,
            AppSecret = PartnerSecret,
            AccessToken = Token,
            Version = 2,
            TimeoutMs = 5000,
            IsUploadWeight = true,
            Type = 1,
            IsUnLid = false,
            Channel = string.Empty,
            DefaultWeight = -1,
            IsEnabled = true,
            Description = "Test configuration",
            CreatedAt = _clock.LocalNow,
            UpdatedAt = _clock.LocalNow
        };
        _configRepoMock.Setup(x => x.GetByIdAsync(JushuitanErpConfig.SingletonId))
            .ReturnsAsync(testConfig);
    }

    private JushuitanErpApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.jushuitan.com")
        };
        return new JushuitanErpApiClient(httpClient, _loggerMock.Object, _clock, _configRepoMock.Object);
    }

    [Fact]
    public async Task RequestChuteAsync_WithDwsData_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"code\":0,\"msg\":\"success\",\"data\":{\"result\":true}}";

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
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("请求格口成功", result.FormattedMessage);
        Assert.Contains("success", result.ResponseBody);
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
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("聚水潭ERP不支持扫描包裹功能", result.FormattedMessage);
    }

    [Fact]
    public async Task RequestChuteAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"code\":0,\"msg\":\"success\",\"data\":{\"result\":true}}";

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
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("请求格口成功", result.FormattedMessage);
        Assert.Contains("success", result.ResponseBody);
    }

    [Fact]
    public async Task UploadImageAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header

        var client = CreateClient(new Mock<HttpMessageHandler>().Object);

        // Act
        var result = await client.UploadImageAsync(barcode, imageData);

        // Assert
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Contains("暂不支持", result.FormattedMessage);
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
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal("ERROR", result.ResponseStatusCode);
        Assert.Contains("Network error", result.FormattedMessage);
    }
}
