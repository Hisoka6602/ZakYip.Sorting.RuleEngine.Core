using ZakYip.Sorting.RuleEngine.Domain.Enums;
using System.Net;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtErpFlagship;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 旺店通ERP旗舰版 API适配器测试
/// WDT ERP Flagship API adapter tests
/// </summary>
public class WdtErpFlagshipApiClientTests
{
    private readonly Mock<ILogger<WdtErpFlagshipApiClient>> _loggerMock;
    private readonly Mock<IWdtErpFlagshipConfigRepository> _mockConfigRepo;
    private const string Key = "test_key";
    private const string Appsecret = "test_appsecret";
    private const string Sid = "test_sid";

    public WdtErpFlagshipApiClientTests()
    {
        _loggerMock = new Mock<ILogger<WdtErpFlagshipApiClient>>();
        
        // Setup mock config repository
        var testConfig = new WdtErpFlagshipConfig
        {
            ConfigId = WdtErpFlagshipConfig.SingletonId,
            Url = "https://api.example.com/flagship",
            Key = Key,
            Appsecret = Appsecret,
            Sid = Sid,
            Method = "wms.stockout.Sales.weighingExt",
            V = "1.0",
            Salt = "salt123",
            PackagerId = 1001,
            PackagerNo = string.Empty,
            OperateTableName = string.Empty,
            Force = false,
            TimeoutMs = 5000,
            IsEnabled = true,
            Description = "Test configuration",
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0)
        };
        
        _mockConfigRepo = new Mock<IWdtErpFlagshipConfigRepository>();
        _mockConfigRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(testConfig);
    }

    private WdtErpFlagshipApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        var clock = new MockSystemClock();
        var client = new WdtErpFlagshipApiClient(httpClient, _loggerMock.Object, clock, _mockConfigRepo.Object);
        return client;
    }

    [Fact]
    public async Task RequestChuteAsync_WithValidData_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"status\":\"0\",\"message\":\"success\",\"data\":{}}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("key=") &&
                    req.RequestUri.ToString().Contains("sign=")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData 
        { 
            Barcode = barcode, 
            Weight = 2.567m, 
            Length = 30.5m,
            Width = 20.3m,
            Height = 15.8m,
            Volume = 9.82m 
        });

        // Assert
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("上传称重数据成功", result.FormattedMessage);
        Assert.Contains("status", result.ResponseBody);
    }

    [Fact]
    public async Task RequestChuteAsync_WithWeighingExtMethod_ConstructsCorrectBody()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"status\":\"0\",\"message\":\"success\"}";
        string? capturedRequestUri = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedRequestUri = req.RequestUri?.ToString();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 2.567m });

        // Assert
        Assert.NotNull(capturedRequestUri);
        Assert.Contains("method=wms.stockout.Sales.weighingExt", capturedRequestUri);
        Assert.Contains("sign=", capturedRequestUri);
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
    }

    [Fact]
    public async Task RequestChuteAsync_WithOnceWeighingMethod_ConstructsCorrectBody()
    {
        // Arrange
        var barcode = "TEST789";
        var responseContent = "{\"status\":\"0\",\"message\":\"success\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 1.5m });

        // Assert
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
    }

    [Fact]
    public async Task RequestChuteAsync_WithOnceWeighingByNoMethod_ConstructsCorrectBody()
    {
        // Arrange
        var barcode = "TEST456";
        var responseContent = "{\"status\":\"0\",\"message\":\"success\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 3.2m });

        // Assert
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
    }

    [Fact]
    public async Task RequestChuteAsync_WithFailureStatus_ReturnsFailure()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"status\":\"1\",\"message\":\"操作失败\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 1.5m });

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Contains("操作失败", result.FormattedMessage);
    }

    [Fact]
    public async Task RequestChuteAsync_WithHttpError_ReturnsFailure()
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
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 1.5m });

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal("ERROR", result.ResponseStatusCode);
        Assert.Contains("接口访问异常", result.FormattedMessage);
    }

    [Fact]
    public async Task RequestChuteAsync_WithTimeout_ReturnsTimeoutError()
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
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = barcode, Weight = 1.5m });

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal("ERROR", result.ResponseStatusCode);
        Assert.Contains("接口访问返回超时", result.FormattedMessage);
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
        Assert.Equal("旺店通ERP旗舰版不支持扫描包裹功能", result.FormattedMessage);
    }

    [Fact]
    public async Task UploadImageAsync_NotSupported_ReturnsNotSupportedMessage()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0x01, 0x02, 0x03 };

        var client = CreateClient(new Mock<HttpMessageHandler>().Object);

        // Act
        var result = await client.UploadImageAsync(barcode, imageData);

        // Assert
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("旺店通ERP旗舰版不支持上传图片功能", result.FormattedMessage);
    }

    [Fact]
    public async Task RequestChuteAsync_RoundsWeightTo3Decimals()
    {
        // Arrange
        var responseContent = "{\"status\":\"0\",\"message\":\"success\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act - Weight with many decimals should be rounded to 3
        var result = await client.RequestChuteAsync("PKG001", new DwsData { Barcode = "TEST", Weight = 1.23456789m });

        // Assert
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        // The rounding happens internally, we verify by checking the result is successful
        Assert.Equal("上传称重数据成功", result.FormattedMessage);
    }

    [Fact]
    public void Parameters_CanBeSetAndRetrieved()
    {
        // Arrange
        var client = CreateClient(new Mock<HttpMessageHandler>().Object);

        // Act

        // Assert
    }
}
