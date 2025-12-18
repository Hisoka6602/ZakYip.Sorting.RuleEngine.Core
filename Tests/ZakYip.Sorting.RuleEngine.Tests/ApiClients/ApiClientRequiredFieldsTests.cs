using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;
using Xunit;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// Tests to verify that all API clients populate required fields (DurationMs, RequestBody, RequestHeaders, FormattedCurl)
/// </summary>
public class ApiClientRequiredFieldsTests
{
    #region Helper Methods

    private static Mock<IJushuitanErpConfigRepository> CreateMockJushuitanErpConfigRepository(MockSystemClock clock)
    {
        var mockConfigRepo = new Mock<IJushuitanErpConfigRepository>();
        var testConfig = new JushuitanErpConfig
        {
            ConfigId = JushuitanErpConfig.SingletonId,
            Name = "Test Config",
            Url = "http://localhost:8080",
            AppKey = "testKey",
            AppSecret = "testSecret",
            AccessToken = "testToken",
            Version = 2,
            TimeoutMs = 5000,
            IsUploadWeight = true,
            Type = 1,
            IsUnLid = false,
            Channel = string.Empty,
            DefaultWeight = -1,
            IsEnabled = true,
            Description = "Test",
            CreatedAt = clock.LocalNow,
            UpdatedAt = clock.LocalNow
        };
        mockConfigRepo.Setup(x => x.GetByIdAsync(JushuitanErpConfig.SingletonId))
            .ReturnsAsync(testConfig);
        return mockConfigRepo;
    }

    private static Mock<IWdtWmsConfigRepository> CreateMockWdtWmsConfigRepository(MockSystemClock clock)
    {
        var mockConfigRepo = new Mock<IWdtWmsConfigRepository>();
        var testConfig = new WdtWmsConfig
        {
            ConfigId = WdtWmsConfig.SingletonId,
            Name = "Test Config",
            Url = "http://localhost:8080",
            Sid = "test_sid",
            AppKey = "testKey",
            AppSecret = "testSecret",
            Method = "wms.logistics.Consign.weigh",
            TimeoutMs = 5000,
            MustIncludeBoxBarcode = false,
            DefaultWeight = 0.0,
            IsEnabled = true,
            Description = "Test",
            CreatedAt = clock.LocalNow,
            UpdatedAt = clock.LocalNow
        };
        mockConfigRepo.Setup(x => x.GetByIdAsync(WdtWmsConfig.SingletonId))
            .ReturnsAsync(testConfig);
        return mockConfigRepo;
    }

    #endregion

    #region WcsApiClient Tests

    [Fact]
    public async Task WcsApiClient_ScanParcelAsync_PopulatesAllRequiredFields()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"success\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        var logger = Mock.Of<ILogger<WcsApiClient>>();
        var client = new WcsApiClient(httpClient, logger, new MockSystemClock());

        // Act
        var result = await client.ScanParcelAsync("TEST12345");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DurationMs >= 0);
        Assert.False(string.IsNullOrEmpty(result.RequestBody));
        Assert.False(string.IsNullOrEmpty(result.RequestHeaders));
        Assert.False(string.IsNullOrEmpty(result.FormattedCurl));
        Assert.False(string.IsNullOrEmpty(result.RequestUrl));
        Assert.NotEqual(default, result.RequestTime);
        Assert.Equal("TEST12345", result.ParcelId);
    }

    [Fact]
    public async Task WcsApiClient_RequestChuteAsync_PopulatesAllRequiredFields()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"chute\":\"A01\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        var logger = Mock.Of<ILogger<WcsApiClient>>();
        var client = new WcsApiClient(httpClient, logger, new MockSystemClock());

        var dwsData = new DwsData
        {
            Barcode = "TEST12345",
            Weight = 1500,
            Volume = 10000,
            Length = 30,
            Width = 20,
            Height = 15
        };

        // Act
        var result = await client.RequestChuteAsync("PKG001", dwsData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DurationMs >= 0);
        Assert.False(string.IsNullOrEmpty(result.RequestBody));
        Assert.False(string.IsNullOrEmpty(result.RequestHeaders));
        Assert.False(string.IsNullOrEmpty(result.FormattedCurl));
        Assert.False(string.IsNullOrEmpty(result.RequestUrl));
        Assert.NotEqual(default, result.RequestTime);
        Assert.Equal("PKG001", result.ParcelId);
    }

    [Fact]
    public async Task WcsApiClient_UploadImageAsync_PopulatesAllRequiredFields()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"uploaded\":true}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        var logger = Mock.Of<ILogger<WcsApiClient>>();
        var client = new WcsApiClient(httpClient, logger, new MockSystemClock());

        var imageData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // Simple image bytes

        // Act
        var result = await client.UploadImageAsync("TEST12345", imageData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DurationMs >= 0);
        Assert.False(string.IsNullOrEmpty(result.RequestBody));
        Assert.False(string.IsNullOrEmpty(result.RequestHeaders));
        Assert.False(string.IsNullOrEmpty(result.FormattedCurl));
        Assert.False(string.IsNullOrEmpty(result.RequestUrl));
        Assert.NotEqual(default, result.RequestTime);
        Assert.Equal("TEST12345", result.ParcelId);
    }

    [Fact]
    public async Task WcsApiClient_ScanParcelAsync_ErrorCase_PopulatesAllRequiredFields()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        var logger = Mock.Of<ILogger<WcsApiClient>>();
        var client = new WcsApiClient(httpClient, logger, new MockSystemClock());

        // Act
        var result = await client.ScanParcelAsync("TEST12345");

        // Assert - Even on error, all fields should be populated
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.True(result.DurationMs >= 0);
        Assert.False(string.IsNullOrEmpty(result.RequestBody));
        Assert.False(string.IsNullOrEmpty(result.RequestHeaders));
        Assert.False(string.IsNullOrEmpty(result.FormattedCurl));
        Assert.False(string.IsNullOrEmpty(result.RequestUrl));
        Assert.NotEqual(default, result.RequestTime);
        Assert.Equal("TEST12345", result.ParcelId);
    }

    #endregion

    #region JushuitanErpApiClient Tests

    [Fact]
    public async Task JushuitanErpApiClient_RequestChuteAsync_PopulatesAllRequiredFields()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"data\":{\"datas\":[{\"is_success\":true}]}}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        var logger = Mock.Of<ILogger<JushuitanErpApiClient>>();
        var clock = new MockSystemClock();
        
        // Setup mock config repository using helper method
        var mockConfigRepo = CreateMockJushuitanErpConfigRepository(clock);
        
        var client = new JushuitanErpApiClient(httpClient, logger, clock, mockConfigRepo.Object);

        var dwsData = new DwsData
        {
            Barcode = "TEST12345",
            Weight = 1500,
            Volume = 10000
        };

        // Act
        var result = await client.RequestChuteAsync("PKG001", dwsData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DurationMs >= 0);
        Assert.False(string.IsNullOrEmpty(result.RequestBody));
        Assert.False(string.IsNullOrEmpty(result.RequestHeaders));
        Assert.False(string.IsNullOrEmpty(result.FormattedCurl));
        Assert.False(string.IsNullOrEmpty(result.RequestUrl));
        Assert.NotEqual(default, result.RequestTime);
        Assert.Equal("PKG001", result.ParcelId);
    }

    [Fact]
    public async Task JushuitanErpApiClient_ScanParcelAsync_PopulatesAllRequiredFields()
    {
        // Arrange
        var logger = Mock.Of<ILogger<JushuitanErpApiClient>>();
        var httpClient = new HttpClient();
        var clock = new MockSystemClock();
        
        // Setup mock config repository using helper method
        var mockConfigRepo = CreateMockJushuitanErpConfigRepository(clock);
        
        var client = new JushuitanErpApiClient(httpClient, logger, clock, mockConfigRepo.Object);

        // Act
        var result = await client.ScanParcelAsync("TEST12345");

        // Assert - Even for unsupported operation, all fields should be populated
        Assert.NotNull(result);
        Assert.True(result.DurationMs >= 0);
        Assert.False(string.IsNullOrEmpty(result.RequestBody));
        Assert.False(string.IsNullOrEmpty(result.RequestHeaders));
        Assert.False(string.IsNullOrEmpty(result.FormattedCurl));
        Assert.NotEqual(default, result.RequestTime);
        Assert.Equal("TEST12345", result.ParcelId);
    }

    #endregion

    #region WdtWmsApiClient Tests

    [Fact]
    public async Task WdtWmsApiClient_RequestChuteAsync_PopulatesAllRequiredFields()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"flag\":\"success\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
        var logger = Mock.Of<ILogger<WdtWmsApiClient>>();
        var clock = new MockSystemClock();
        
        // Setup mock config repository using helper method
        var mockConfigRepo = CreateMockWdtWmsConfigRepository(clock);
        
        var client = new WdtWmsApiClient(httpClient, logger, clock, mockConfigRepo.Object);

        var dwsData = new DwsData
        {
            Barcode = "TEST12345",
            Weight = 1500,
            Volume = 10000
        };

        // Act
        var result = await client.RequestChuteAsync("PKG001", dwsData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.DurationMs >= 0);
        Assert.False(string.IsNullOrEmpty(result.RequestBody));
        Assert.False(string.IsNullOrEmpty(result.RequestHeaders));
        Assert.False(string.IsNullOrEmpty(result.FormattedCurl));
        Assert.False(string.IsNullOrEmpty(result.RequestUrl));
        Assert.NotEqual(default, result.RequestTime);
        Assert.Equal("PKG001", result.ParcelId);
    }

    [Fact]
    public async Task WdtWmsApiClient_ScanParcelAsync_PopulatesAllRequiredFields()
    {
        // Arrange
        var logger = Mock.Of<ILogger<WdtWmsApiClient>>();
        var httpClient = new HttpClient();
        var clock = new MockSystemClock();
        
        // Setup mock config repository using helper method
        var mockConfigRepo = CreateMockWdtWmsConfigRepository(clock);
        
        var client = new WdtWmsApiClient(httpClient, logger, clock, mockConfigRepo.Object);

        // Act
        var result = await client.ScanParcelAsync("TEST12345");

        // Assert - Even for unsupported operation, all fields should be populated
        Assert.NotNull(result);
        Assert.True(result.DurationMs >= 0);
        Assert.False(string.IsNullOrEmpty(result.RequestBody));
        Assert.False(string.IsNullOrEmpty(result.RequestHeaders));
        Assert.False(string.IsNullOrEmpty(result.FormattedCurl));
        Assert.NotEqual(default, result.RequestTime);
        Assert.Equal("TEST12345", result.ParcelId);
    }

    #endregion
}
