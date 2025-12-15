using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
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
    private const string PartnerKey = "test_partner_key";
    private const string PartnerSecret = "test_partner_secret";
    private const string Token = "test_token";

    public JushuitanErpApiClientTests()
    {
        _loggerMock = new Mock<ILogger<JushuitanErpApiClient>>();
    }

    private JushuitanErpApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.jushuitan.com")
        };
        var clock = new MockSystemClock();
        return new JushuitanErpApiClient(httpClient, _loggerMock.Object, clock, PartnerKey, PartnerSecret, Token);
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
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("请求格口成功", result.Message);
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
        Assert.Equal("聚水潭ERP不支持扫描包裹功能", result.Message);
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
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("请求格口成功", result.Message);
        Assert.Contains("success", result.Data);
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
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Contains("暂不支持", result.Message);
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
