using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Adapters.Post;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 邮政分揽投机构API适配器测试
/// Postal Collection API adapter tests
/// </summary>
public class PostCollectionApiAdapterTests
{
    private readonly Mock<ILogger<PostCollectionApiAdapter>> _loggerMock;

    public PostCollectionApiAdapterTests()
    {
        _loggerMock = new Mock<ILogger<PostCollectionApiAdapter>>();
    }

    private PostCollectionApiAdapter CreateAdapter(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.postal-collection.example.com")
        };
        return new PostCollectionApiAdapter(httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task UploadWeighingDataAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var parcelData = new PostalParcelData
        {
            Barcode = "POST123456",
            Weight = 1500.5m,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000,
            SenderAddress = "Beijing",
            RecipientAddress = "Shanghai",
            DestinationCode = "SH001"
        };
        var responseContent = "{\"success\":true,\"messageId\":\"MSG123\"}";

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
        var responseContent = "{\"error\":\"Invalid data\"}";

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
    public async Task QueryParcelAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "POST123456";
        var responseContent = "{\"barcode\":\"POST123456\",\"status\":\"In Transit\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/parcel/query")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.QueryParcelAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal("Parcel query successful", result.Message);
        Assert.Contains("barcode", result.Data);
    }

    [Fact]
    public async Task QueryParcelAsync_NotFound_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "NOTFOUND123";
        var responseContent = "{\"error\":\"Parcel not found\"}";

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
        var result = await adapter.QueryParcelAsync(barcode);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("404", result.Code);
        Assert.Contains("Query Error", result.Message);
    }

    [Fact]
    public async Task UploadScanDataAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "POST123456";
        var scanTime = new DateTime(2024, 1, 1, 12, 0, 0);
        var responseContent = "{\"success\":true,\"scanId\":\"SCAN123\"}";

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
        var barcode = "POST123456";
        var scanTime = new DateTime(2024, 1, 1, 12, 0, 0);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var adapter = CreateAdapter(handlerMock.Object);

        // Act
        var result = await adapter.UploadScanDataAsync(barcode, scanTime);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Network error", result.Message);
    }
}
