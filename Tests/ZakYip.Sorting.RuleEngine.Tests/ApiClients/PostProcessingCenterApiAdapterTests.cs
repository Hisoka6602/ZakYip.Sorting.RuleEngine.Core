using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Post;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 邮政处理中心API适配器测试
/// Postal Processing Center API adapter tests
/// </summary>
public class PostProcessingCenterApiAdapterTests
{
    private readonly Mock<ILogger<PostProcessingCenterApiAdapter>> _loggerMock;

    public PostProcessingCenterApiAdapterTests()
    {
        _loggerMock = new Mock<ILogger<PostProcessingCenterApiAdapter>>();
    }

    private PostProcessingCenterApiAdapter CreateAdapter(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.postal-processing.example.com")
        };
        return new PostProcessingCenterApiAdapter(httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task UploadWeighingDataAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var parcelData = new PostalParcelData
        {
            Barcode = "PROC123456",
            Weight = 2500.75m,
            Length = 400,
            Width = 300,
            Height = 200,
            Volume = 24000,
            SenderAddress = "Guangzhou",
            RecipientAddress = "Shenzhen",
            DestinationCode = "SZ001"
        };
        var responseContent = "{\"success\":true,\"messageId\":\"MSG456\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/weighing/upload")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.UploadWeighingDataAsync(parcelData);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("Weighing data uploaded successfully", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task UploadWeighingDataAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var parcelData = new PostalParcelData
        {
            Barcode = "INVALID",
            Weight = 0
        };
        var responseContent = "{\"error\":\"Invalid weight data\"}";

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

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.UploadWeighingDataAsync(parcelData);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("400", result.Code);
        Assert.Contains("Upload Error", result.Message);
    }

    [Fact]
    public async Task QueryParcelRoutingAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "PROC123456";
        var responseContent = "{\"barcode\":\"PROC123456\",\"route\":[\"Hub1\",\"Hub2\",\"Destination\"]}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/routing/query")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.QueryParcelRoutingAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("Parcel routing query successful", result.Message);
        Assert.Contains("route", result.Data);
    }

    [Fact]
    public async Task QueryParcelRoutingAsync_NotFound_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "NOTFOUND456";
        var responseContent = "{\"error\":\"Routing not found\"}";

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

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.QueryParcelRoutingAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("404", result.Code);
        Assert.Contains("Query Error", result.Message);
    }

    [Fact]
    public async Task UploadSortingResultAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "PROC123456";
        var destinationCode = "SZ001";
        var chuteNumber = "CH05";
        var responseContent = "{\"success\":true,\"sortingId\":\"SORT123\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/sorting/result")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.UploadSortingResultAsync(barcode, destinationCode, chuteNumber);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("Sorting result uploaded successfully", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task UploadSortingResultAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "INVALID";
        var destinationCode = "";
        var chuteNumber = "";
        var responseContent = "{\"error\":\"Invalid sorting data\"}";

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

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.UploadSortingResultAsync(barcode, destinationCode, chuteNumber);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("400", result.Code);
        Assert.Contains("Upload Error", result.Message);
    }

    [Fact]
    public async Task UploadScanDataAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "PROC123456";
        var scanTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var responseContent = "{\"success\":true,\"scanId\":\"SCAN456\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/scan/upload")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.UploadScanDataAsync(barcode, scanTime);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("Scan data uploaded successfully", result.Message);
        Assert.Contains("success", result.Data);
    }

    [Fact]
    public async Task UploadScanDataAsync_Exception_ReturnsErrorResponse()
    {
        // Arrange
        var barcode = "PROC123456";
        var scanTime = new DateTime(2024, 1, 1, 12, 0, 0);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection timeout"));

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.UploadScanDataAsync(barcode, scanTime);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Connection timeout", result.Message);
    }
}
