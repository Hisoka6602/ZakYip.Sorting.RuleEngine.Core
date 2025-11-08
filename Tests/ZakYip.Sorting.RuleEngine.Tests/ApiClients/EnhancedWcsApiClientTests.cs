using Xunit;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using System.Net;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// 增强版WCS API客户端测试
/// Tests for EnhancedWcsApiClient
/// </summary>
public class EnhancedWcsApiClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<EnhancedWcsApiClient>> _mockLogger;
    private readonly EnhancedWcsApiClient _client;

    public EnhancedWcsApiClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        _mockLogger = new Mock<ILogger<EnhancedWcsApiClient>>();
        _client = new EnhancedWcsApiClient(_httpClient, _mockLogger.Object);
    }

    /// <summary>
    /// 测试扫描包裹 - 成功场景
    /// Test ScanParcelAsync - Success scenario
    /// </summary>
    [Fact]
    public async Task ScanParcelAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"parcelId\":\"TEST123456\",\"barcode\":\"TEST123456\",\"isRegistered\":true}";
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _client.ScanParcelAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal(barcode, result.ParcelId);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// 测试扫描包裹（强类型） - 成功场景
    /// Test ScanParcelStronglyTypedAsync - Success scenario
    /// </summary>
    [Fact]
    public async Task ScanParcelStronglyTypedAsync_Success_ReturnsTypedResponse()
    {
        // Arrange
        var barcode = "TEST123456";
        var responseContent = "{\"parcelId\":\"TEST123456\",\"barcode\":\"TEST123456\",\"isRegistered\":true,\"scanTime\":\"2024-01-01T10:00:00Z\"}";
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _client.ScanParcelStronglyTypedAsync(barcode);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(barcode, result.Data.Barcode);
        Assert.True(result.Data.IsRegistered);
    }

    /// <summary>
    /// 测试请求格口 - 成功场景
    /// Test RequestChuteAsync - Success scenario
    /// </summary>
    [Fact]
    public async Task RequestChuteAsync_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var parcelId = "PARCEL001";
        var dwsData = new DwsData
        {
            Barcode = "TEST123456",
            Weight = 1000,
            Length = 30,
            Width = 20,
            Height = 10,
            Volume = 6000,
            ScannedAt = DateTime.Now
        };
        var responseContent = "{\"chuteNumber\":\"A001\",\"chuteName\":\"Chute A001\",\"parcelId\":\"PARCEL001\"}";
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _client.RequestChuteAsync(parcelId, dwsData);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("200", result.Code);
        Assert.Equal(parcelId, result.ParcelId);
    }

    /// <summary>
    /// 测试请求格口（强类型） - 成功场景
    /// Test RequestChuteStronglyTypedAsync - Success scenario
    /// </summary>
    [Fact]
    public async Task RequestChuteStronglyTypedAsync_Success_ReturnsTypedResponse()
    {
        // Arrange
        var parcelId = "PARCEL001";
        var dwsData = new DwsData
        {
            Barcode = "TEST123456",
            Weight = 1000,
            Length = 30,
            Width = 20,
            Height = 10,
            Volume = 6000,
            ScannedAt = DateTime.Now
        };
        var responseContent = "{\"chuteNumber\":\"A001\",\"chuteName\":\"Chute A001\",\"parcelId\":\"PARCEL001\",\"barcode\":\"TEST123456\"}";
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _client.RequestChuteStronglyTypedAsync(parcelId, dwsData);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("A001", result.Data.ChuteNumber);
        Assert.Equal("Chute A001", result.Data.ChuteName);
    }

    /// <summary>
    /// 测试批量请求格口 - 成功场景
    /// Test BatchRequestChuteAsync - Success scenario
    /// </summary>
    [Fact]
    public async Task BatchRequestChuteAsync_Success_ReturnsBatchResponse()
    {
        // Arrange
        var requests = new BatchOperationRequest<(string ParcelId, DwsData DwsData, OcrData? OcrData)>
        {
            Requests = new List<(string, DwsData, OcrData?)>
            {
                ("PARCEL001", new DwsData { Barcode = "TEST001", Weight = 1000, Length = 30, Width = 20, Height = 10, Volume = 6000, ScannedAt = DateTime.Now }, null),
                ("PARCEL002", new DwsData { Barcode = "TEST002", Weight = 2000, Length = 40, Width = 30, Height = 20, Volume = 24000, ScannedAt = DateTime.Now }, null),
                ("PARCEL003", new DwsData { Barcode = "TEST003", Weight = 1500, Length = 35, Width = 25, Height = 15, Volume = 13125, ScannedAt = DateTime.Now }, null)
            },
            ProcessInParallel = true,
            MaxDegreeOfParallelism = 2
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"chuteNumber\":\"A001\"}")
            });

        // Act
        var result = await _client.BatchRequestChuteAsync(requests);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.NotEmpty(result.SuccessfulResponses);
    }

    /// <summary>
    /// 测试批量上传图片 - 成功场景
    /// Test BatchUploadImageAsync - Success scenario
    /// </summary>
    [Fact]
    public async Task BatchUploadImageAsync_Success_ReturnsBatchResponse()
    {
        // Arrange
        var imageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        var requests = new BatchOperationRequest<(string Barcode, byte[] ImageData, string ContentType)>
        {
            Requests = new List<(string, byte[], string)>
            {
                ("TEST001", imageData, "image/png"),
                ("TEST002", imageData, "image/png")
            },
            ProcessInParallel = false
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"imageId\":\"IMG001\"}")
            });

        // Act
        var result = await _client.BatchUploadImageAsync(requests);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
    }

    /// <summary>
    /// 测试请求格口 - 失败场景
    /// Test RequestChuteAsync - Failure scenario
    /// </summary>
    [Fact]
    public async Task RequestChuteAsync_Failure_ReturnsErrorResponse()
    {
        // Arrange
        var parcelId = "PARCEL001";
        var dwsData = new DwsData
        {
            Barcode = "TEST123456",
            Weight = 1000,
            Length = 30,
            Width = 20,
            Height = 10,
            Volume = 6000,
            ScannedAt = DateTime.Now
        };
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("{\"error\":\"Internal server error\"}")
            });

        // Act
        var result = await _client.RequestChuteAsync(parcelId, dwsData);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("500", result.Code);
        Assert.NotNull(result.ErrorMessage);
    }

    /// <summary>
    /// 测试请求格口 - 异常场景
    /// Test RequestChuteAsync - Exception scenario
    /// </summary>
    [Fact]
    public async Task RequestChuteAsync_Exception_ReturnsErrorResponse()
    {
        // Arrange
        var parcelId = "PARCEL001";
        var dwsData = new DwsData
        {
            Barcode = "TEST123456",
            Weight = 1000,
            Length = 30,
            Width = 20,
            Height = 10,
            Volume = 6000,
            ScannedAt = DateTime.Now
        };
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _client.RequestChuteAsync(parcelId, dwsData);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("ERROR", result.Code);
        Assert.Contains("Network error", result.ErrorMessage);
    }
}
