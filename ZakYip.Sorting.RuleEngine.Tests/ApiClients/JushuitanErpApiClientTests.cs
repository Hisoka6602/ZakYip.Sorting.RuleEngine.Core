using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 聚水潭ERP API客户端测试
/// Jushuituan ERP API client tests
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
        return new JushuitanErpApiClient(httpClient, _loggerMock.Object, PartnerKey, PartnerSecret, Token);
    }

    [Fact]
    public async Task WeightCallbackAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var weight = 1.5m;
        var length = 30m;
        var width = 20m;
        var height = 10m;
        var responseContent = "{\"code\":0,\"msg\":\"success\",\"data\":{\"result\":true}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/open/api/weigh/upload")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.WeightCallbackAsync(barcode, weight, length, width, height);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("称重回传成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task WeightCallbackAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "INVALID";
        var weight = 1.5m;
        var length = 30m;
        var width = 20m;
        var height = 10m;
        var responseContent = "{\"code\":1001,\"msg\":\"Invalid order\"}";

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
        var result = await client.WeightCallbackAsync(barcode, weight, length, width, height);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("400", result.Code);
        Assert.Contains("称重回传失败", result.Message);
    }

    [Fact]
    public async Task QueryOrderAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"code\":0,\"msg\":\"success\",\"data\":{\"orders\":[{\"so_id\":\"TEST123456\"}]}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/open/api/orders/query")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.QueryOrderAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("查询订单成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task QueryOrderAsync_NotFound_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "NOTFOUND";
        var responseContent = "{\"code\":1002,\"msg\":\"Order not found\"}";

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
        var result = await client.QueryOrderAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("404", result.Code);
        Assert.Contains("查询订单失败", result.Message);
    }

    [Fact]
    public async Task UpdateLogisticsAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var logisticsCompany = "SF";
        var trackingNumber = "SF123456789";
        var responseContent = "{\"code\":0,\"msg\":\"success\",\"data\":{\"result\":true}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/open/api/logistic/update")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.UpdateLogisticsAsync(barcode, logisticsCompany, trackingNumber);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("更新物流成功", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task UpdateLogisticsAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var logisticsCompany = "SF";
        var trackingNumber = "INVALID";
        var responseContent = "{\"code\":1003,\"msg\":\"Invalid tracking number\"}";

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
        var result = await client.UpdateLogisticsAsync(barcode, logisticsCompany, trackingNumber);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("400", result.Code);
        Assert.Contains("更新物流失败", result.Message);
    }

    [Fact]
    public async Task WeightCallbackAsync_Exception_ReturnsErrorResponse()
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
        var result = await client.WeightCallbackAsync(barcode, weight, length, width, height);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Network error", result.Message);
    }

    [Fact]
    public async Task QueryOrderAsync_Exception_ReturnsErrorResponse()
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
        var result = await client.QueryOrderAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Request timeout", result.Message);
    }

    [Fact]
    public async Task UpdateLogisticsAsync_Exception_ReturnsErrorResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var logisticsCompany = "SF";
        var trackingNumber = "SF123456789";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Update failed"));

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.UpdateLogisticsAsync(barcode, logisticsCompany, trackingNumber);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Update failed", result.Message);
    }
}
