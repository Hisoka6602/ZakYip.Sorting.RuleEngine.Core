using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 旺店通WMS API适配器测试
/// WDT WMS API adapter tests
/// </summary>
public class WdtWmsApiAdapterTests
{
    private readonly Mock<ILogger<WdtWmsApiAdapter>> _loggerMock;
    private const string AppKey = "test_app_key";
    private const string AppSecret = "test_app_secret";

    public WdtWmsApiAdapterTests()
    {
        _loggerMock = new Mock<ILogger<WdtWmsApiAdapter>>();
    }

    private WdtWmsApiAdapter CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.wdt.com")
        };
        return new WdtWmsApiAdapter(httpClient, _loggerMock.Object, AppKey, AppSecret);
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
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/openapi/router")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("查询包裹成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task ScanParcelAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"code\":0,\"message\":\"success\",\"data\":{\"status\":\"processed\"}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/openapi/router")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.ScanParcelAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("扫描包裹成功", result.Message);
        Assert.Contains("success", result.Data);
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
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/openapi/router")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync(barcode);

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
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/openapi/router")),
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
        var result = await client.RequestChuteAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Network error", result.Message);
    }
}
