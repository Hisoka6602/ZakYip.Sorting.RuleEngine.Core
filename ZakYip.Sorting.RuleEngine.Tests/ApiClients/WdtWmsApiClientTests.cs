using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 旺店通WMS API客户端测试
/// WDT WMS API client tests
/// </summary>
public class WdtWmsApiClientTests
{
    private readonly Mock<ILogger<WdtWmsApiClient>> _loggerMock;
    private const string AppKey = "test_app_key";
    private const string AppSecret = "test_app_secret";

    public WdtWmsApiClientTests()
    {
        _loggerMock = new Mock<ILogger<WdtWmsApiClient>>();
    }

    private WdtWmsApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.wdt.com")
        };
        return new WdtWmsApiClient(httpClient, _loggerMock.Object, AppKey, AppSecret);
    }

    [Fact]
    public async Task WeighScanAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var weight = 1.5m;
        var length = 30m;
        var width = 20m;
        var height = 10m;
        var responseContent = "{\"code\":0,\"message\":\"success\",\"data\":{\"parcelId\":\"12345\"}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/openapi/weigh/scan")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.WeighScanAsync(barcode, weight, length, width, height);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("称重扫描成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task WeighScanAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "INVALID";
        var weight = 1.5m;
        var length = 30m;
        var width = 20m;
        var height = 10m;
        var responseContent = "{\"code\":1001,\"message\":\"Invalid barcode\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.WeighScanAsync(barcode, weight, length, width, height);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("400", result.Code);
        Assert.Contains("称重扫描失败", result.Message);
    }

    [Fact]
    public async Task QueryParcelAsync_Success_ReturnsSuccessResponse()
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
                    req.RequestUri!.ToString().Contains("/openapi/parcel/query")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.QueryParcelAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("查询包裹成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task QueryParcelAsync_NotFound_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "NOTFOUND";
        var responseContent = "{\"code\":1002,\"message\":\"Parcel not found\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.QueryParcelAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("404", result.Code);
        Assert.Contains("查询包裹失败", result.Message);
    }

    [Fact]
    public async Task UploadParcelImageAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        var imageType = "top";
        var responseContent = "{\"code\":0,\"message\":\"success\",\"data\":{\"imageId\":\"img_123\"}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/openapi/parcel/image")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.UploadParcelImageAsync(barcode, imageData, imageType);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("上传图片成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task UploadParcelImageAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0x00, 0x00 }; // Invalid image
        var imageType = "top";
        var responseContent = "{\"code\":1003,\"message\":\"Invalid image format\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.UploadParcelImageAsync(barcode, imageData, imageType);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("400", result.Code);
        Assert.Contains("上传图片失败", result.Message);
    }

    [Fact]
    public async Task WeighScanAsync_Exception_ReturnsErrorResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var weight = 1.5m;
        var length = 30m;
        var width = 20m;
        var height = 10m;

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
        var result = await client.WeighScanAsync(barcode, weight, length, width, height);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Network error", result.Message);
    }

    [Fact]
    public async Task QueryParcelAsync_Exception_ReturnsErrorResponse()
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
            .ThrowsAsync(new TimeoutException("Request timeout"));

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.QueryParcelAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Request timeout", result.Message);
    }

    [Fact]
    public async Task UploadParcelImageAsync_Exception_ReturnsErrorResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var imageType = "top";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Upload failed"));

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.UploadParcelImageAsync(barcode, imageData, imageType);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Upload failed", result.Message);
    }
}
