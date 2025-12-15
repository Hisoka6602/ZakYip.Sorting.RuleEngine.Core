using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using Moq;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using Moq.Protected;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using System.Net;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using System.Text;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.JushuitanErp;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.WdtWms;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;
using Xunit;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// Tests to verify that all API clients populate required fields (DurationMs, RequestBody, RequestHeaders, FormattedCurl)
/// </summary>
public class ApiClientRequiredFieldsTests
{
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
        var client = new JushuitanErpApiClient(httpClient, logger, new MockSystemClock(), "testKey", "testSecret", "testToken");

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
        var client = new JushuitanErpApiClient(httpClient, logger, new MockSystemClock(), "testKey", "testSecret", "testToken");

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
        var client = new WdtWmsApiClient(httpClient, logger, "testKey", "testSecret", "testSid");

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
        var client = new WdtWmsApiClient(httpClient, logger, "testKey", "testSecret", "testSid");

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
