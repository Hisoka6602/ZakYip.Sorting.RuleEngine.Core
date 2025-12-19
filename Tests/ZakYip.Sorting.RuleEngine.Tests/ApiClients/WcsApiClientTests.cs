using ZakYip.Sorting.RuleEngine.Domain.Enums;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// WCS API客户端测试
/// WCS API client tests
/// </summary>
public class WcsApiClientTests
{
    private readonly Mock<ILogger<WcsApiClient>> _loggerMock;

    public WcsApiClientTests()
    {
        _loggerMock = new Mock<ILogger<WcsApiClient>>();
    }

    private WcsApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        var clock = new MockSystemClock();
        return new WcsApiClient(httpClient, _loggerMock.Object, clock);
    }

    [Fact]
    public async Task ScanParcelAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"success\":true,\"parcelId\":\"12345\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/parcel/scan")),
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
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("Parcel scanned successfully", result.FormattedMessage);
        Assert.Contains("success", result.ResponseBody);
    }

    [Fact]
    public async Task ScanParcelAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "INVALID";
        var responseContent = "{\"error\":\"Invalid barcode\"}";

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
        var result = await client.ScanParcelAsync(barcode);

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(400, result.ResponseStatusCode.Value);
        Assert.Contains("Scan Error", result.FormattedMessage);
    }

    [Fact]
    public async Task RequestChuteAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var parcelId = "PKG001";
        var barcode = "TEST123456";
        var dwsData = new DwsData
        {
            Barcode = barcode,
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };
        var responseContent = "{\"success\":true,\"chuteNumber\":\"A-101\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/chute/request")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync(parcelId, dwsData);

        // Assert
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("Chute requested successfully", result.FormattedMessage);
        Assert.Contains("chuteNumber", result.ResponseBody);
    }

    [Fact]
    public async Task RequestChuteAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var parcelId = "PKG002";
        var barcode = "NOTFOUND";
        var dwsData = new DwsData
        {
            Barcode = barcode,
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };
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

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.RequestChuteAsync(parcelId, dwsData);

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(404, result.ResponseStatusCode.Value);
        Assert.Contains("Chute Request Error", result.FormattedMessage);
    }

    [Fact]
    public async Task UploadImageAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG header
        var responseContent = "{\"success\":true,\"imageId\":\"img_123\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/image/upload")),
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
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("Image uploaded successfully", result.FormattedMessage);
        Assert.Contains("success", result.ResponseBody);
    }

    [Fact]
    public async Task UploadImageAsync_WithPngContentType_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        var responseContent = "{\"success\":true,\"imageId\":\"img_123\"}";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/image/upload")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var client = CreateClient(handlerMock.Object);

        // Act
        var result = await client.UploadImageAsync(barcode, imageData, "image/png");

        // Assert
        Assert.True(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(200, result.ResponseStatusCode.Value);
        Assert.Equal("Image uploaded successfully", result.FormattedMessage);
        Assert.Contains("success", result.ResponseBody);
    }

    [Fact]
    public async Task UploadImageAsync_Error_ReturnsFailureResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0x00, 0x00 }; // Invalid image
        var responseContent = "{\"error\":\"Invalid image format\"}";

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
        var result = await client.UploadImageAsync(barcode, imageData);

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Equal(400, result.ResponseStatusCode.Value);
        Assert.Contains("Image Upload Error", result.FormattedMessage);
    }

    [Fact]
    public async Task ScanParcelAsync_Exception_ReturnsErrorResponse()
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
        var result = await client.ScanParcelAsync(barcode);

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Contains("Network error", result.FormattedMessage);
    }

    [Fact]
    public async Task RequestChuteAsync_Exception_ReturnsErrorResponse()
    {
        // Arrange
        var parcelId = "PKG003";
        var barcode = "TEST123456";
        var dwsData = new DwsData
        {
            Barcode = barcode,
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

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
        var result = await client.RequestChuteAsync(parcelId, dwsData);

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Contains("Request timeout", result.FormattedMessage);
    }

    [Fact]
    public async Task UploadImageAsync_Exception_ReturnsErrorResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

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
        var result = await client.UploadImageAsync(barcode, imageData);

        // Assert
        Assert.False(result.RequestStatus == ApiRequestStatus.Success);
        Assert.Contains("Upload failed", result.FormattedMessage);
    }
}
