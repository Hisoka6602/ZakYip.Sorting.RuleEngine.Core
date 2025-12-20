using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Service.API;

namespace ZakYip.Sorting.RuleEngine.Tests.Controllers;

/// <summary>
/// ApiClient测试控制器单元测试
/// Unit tests for ApiClientTestController
/// </summary>
public class ApiClientTestControllerTests
{
    private readonly Mock<ILogger<ApiClientTestController>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IWcsApiAdapter> _mockWcsApiAdapter;
    private readonly ApiClientTestController _controller;

    public ApiClientTestControllerTests()
    {
        _mockLogger = new Mock<ILogger<ApiClientTestController>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockWcsApiAdapter = new Mock<IWcsApiAdapter>();

        // Setup mock WcsApiAdapter in service provider
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWcsApiAdapter)))
            .Returns(_mockWcsApiAdapter.Object);

        _controller = new ApiClientTestController(
            _mockLogger.Object,
            _mockServiceProvider.Object
        );

        // Setup HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region ScanParcel Method Tests

    [Fact]
    public async Task TestApiClient_WithScanParcelMethod_CallsScanParcelAsync()
    {
        // Arrange
        var request = new ApiClientTestRequest
        {
            Barcode = "TEST123456789",
            Weight = 500,
            MethodName = WcsApiMethod.ScanParcel
        };

        var expectedResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            ResponseStatusCode = 200,
            FormattedMessage = "扫描成功",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 100
        };

        _mockWcsApiAdapter
            .Setup(a => a.ScanParcelAsync(request.Barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await InvokeTestMethod(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ApiClientTestResponse>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        _mockWcsApiAdapter.Verify(
            a => a.ScanParcelAsync(request.Barcode, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region RequestChute Method Tests

    [Fact]
    public async Task TestApiClient_WithRequestChuteMethod_CallsRequestChuteAsync()
    {
        // Arrange
        var request = new ApiClientTestRequest
        {
            Barcode = "TEST123456789",
            Weight = 500,
            Length = 30,
            Width = 20,
            Height = 15,
            MethodName = WcsApiMethod.RequestChute
        };

        var expectedResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            ResponseStatusCode = 200,
            FormattedMessage = "请求格口成功",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 150
        };

        _mockWcsApiAdapter
            .Setup(a => a.RequestChuteAsync(
                request.Barcode,
                It.IsAny<DwsData>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await InvokeTestMethod(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ApiClientTestResponse>>(okResult.Value);
        Assert.True(response.Success);

        _mockWcsApiAdapter.Verify(
            a => a.RequestChuteAsync(
                request.Barcode,
                It.Is<DwsData>(d => d.Barcode == request.Barcode && d.Weight == request.Weight),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TestApiClient_WithNullMethodName_DefaultsToRequestChute()
    {
        // Arrange
        var request = new ApiClientTestRequest
        {
            Barcode = "TEST123456789",
            Weight = 500,
            MethodName = null // 未指定方法，应默认使用 RequestChute
        };

        var expectedResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            ResponseStatusCode = 200,
            FormattedMessage = "请求格口成功",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 150
        };

        _mockWcsApiAdapter
            .Setup(a => a.RequestChuteAsync(
                request.Barcode,
                It.IsAny<DwsData>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await InvokeTestMethod(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ApiClientTestResponse>>(okResult.Value);
        Assert.True(response.Success);

        // 验证调用的是 RequestChute 方法
        _mockWcsApiAdapter.Verify(
            a => a.RequestChuteAsync(
                It.IsAny<string>(),
                It.IsAny<DwsData>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        // 验证没有调用其他方法
        _mockWcsApiAdapter.Verify(
            a => a.ScanParcelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockWcsApiAdapter.Verify(
            a => a.NotifyChuteLandingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region NotifyChuteLanding Method Tests

    [Fact]
    public async Task TestApiClient_WithNotifyChuteLandingMethod_CallsNotifyChuteLandingAsync()
    {
        // Arrange
        var request = new ApiClientTestRequest
        {
            Barcode = "TEST123456789",
            Weight = 500,
            ParcelId = "PARCEL001",
            ChuteId = "CHUTE_A01",
            MethodName = WcsApiMethod.NotifyChuteLanding
        };

        var expectedResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            ResponseStatusCode = 200,
            FormattedMessage = "落格回调成功",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 120
        };

        _mockWcsApiAdapter
            .Setup(a => a.NotifyChuteLandingAsync(
                request.ParcelId!,
                request.ChuteId!,
                request.Barcode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await InvokeTestMethod(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ApiClientTestResponse>>(okResult.Value);
        Assert.True(response.Success);

        _mockWcsApiAdapter.Verify(
            a => a.NotifyChuteLandingAsync(
                request.ParcelId!,
                request.ChuteId!,
                request.Barcode,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TestApiClient_NotifyChuteLanding_WithNullParcelId_UsesBarcodeAsDefault()
    {
        // Arrange
        var request = new ApiClientTestRequest
        {
            Barcode = "TEST123456789",
            Weight = 500,
            ParcelId = null, // ParcelId 为 null，应使用 Barcode
            ChuteId = "CHUTE_A01",
            MethodName = WcsApiMethod.NotifyChuteLanding
        };

        var expectedResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            ResponseStatusCode = 200,
            FormattedMessage = "落格回调成功",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 120
        };

        _mockWcsApiAdapter
            .Setup(a => a.NotifyChuteLandingAsync(
                request.Barcode, // 应该使用 Barcode 作为 ParcelId
                request.ChuteId!,
                request.Barcode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await InvokeTestMethod(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ApiClientTestResponse>>(okResult.Value);
        Assert.True(response.Success);

        _mockWcsApiAdapter.Verify(
            a => a.NotifyChuteLandingAsync(
                request.Barcode,
                request.ChuteId!,
                request.Barcode,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TestApiClient_NotifyChuteLanding_WithNullChuteId_UsesDefaultChute()
    {
        // Arrange
        var request = new ApiClientTestRequest
        {
            Barcode = "TEST123456789",
            Weight = 500,
            ParcelId = "PARCEL001",
            ChuteId = null, // ChuteId 为 null，应使用默认值
            MethodName = WcsApiMethod.NotifyChuteLanding
        };

        var expectedResponse = new WcsApiResponse
        {
            RequestStatus = ApiRequestStatus.Success,
            ResponseStatusCode = 200,
            FormattedMessage = "落格回调成功",
            RequestTime = DateTime.Now,
            ResponseTime = DateTime.Now,
            DurationMs = 120
        };

        _mockWcsApiAdapter
            .Setup(a => a.NotifyChuteLandingAsync(
                request.ParcelId!,
                "DEFAULT_CHUTE", // 应该使用默认值
                request.Barcode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await InvokeTestMethod(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ApiClientTestResponse>>(okResult.Value);
        Assert.True(response.Success);

        _mockWcsApiAdapter.Verify(
            a => a.NotifyChuteLandingAsync(
                request.ParcelId!,
                "DEFAULT_CHUTE",
                request.Barcode,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task TestApiClient_WithInvalidEnumValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var request = new ApiClientTestRequest
        {
            Barcode = "TEST123456789",
            Weight = 500,
            MethodName = (WcsApiMethod)999 // 无效的枚举值
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await InvokeTestMethod(request));
    }

    [Fact]
    public async Task TestApiClient_WhenApiClientNotConfigured_ReturnsNotFound()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Returns(null); // 返回 null 表示未配置

        var controller = new ApiClientTestController(
            _mockLogger.Object,
            mockServiceProvider.Object
        );

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new ApiClientTestRequest
        {
            Barcode = "TEST123456789",
            Weight = 500,
            MethodName = WcsApiMethod.ScanParcel
        };

        // Act
        var result = await InvokeTestMethodOnController(controller, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<ApiClientTestResponse>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Contains("未配置", response.ErrorMessage);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 辅助方法：调用测试方法（使用反射访问私有方法）
    /// Helper method: Invoke test method (using reflection to access private method)
    /// </summary>
    private async Task<ActionResult<ApiResponse<ApiClientTestResponse>>> InvokeTestMethod(
        ApiClientTestRequest request)
    {
        return await InvokeTestMethodOnController(_controller, request);
    }

    /// <summary>
    /// 辅助方法：在指定控制器上调用测试方法
    /// Helper method: Invoke test method on specified controller
    /// </summary>
    private async Task<ActionResult<ApiResponse<ApiClientTestResponse>>> InvokeTestMethodOnController(
        ApiClientTestController controller,
        ApiClientTestRequest request)
    {
        // 使用反射调用私有的 TestApiClientAsync 方法
        var method = typeof(ApiClientTestController).GetMethod(
            "TestApiClientAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        var task = method!.Invoke(
            controller,
            new object[]
            {
                _mockWcsApiAdapter.Object,
                "TestClient",
                "测试客户端",
                request,
                (Func<IWcsApiAdapter, string, DwsData, OcrData?, CancellationToken, Task<WcsApiResponse>>)
                    ((client, barcode, dwsData, ocrData, ct) =>
                        client.RequestChuteAsync(barcode, dwsData, ocrData, ct))
            }) as Task<ActionResult<ApiResponse<ApiClientTestResponse>>>;

        Assert.NotNull(task);
        return await task!;
    }

    #endregion
}
