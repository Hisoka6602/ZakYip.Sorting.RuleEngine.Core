using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Responses;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Service.API;
using ZakYip.Sorting.RuleEngine.Service.Configuration;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.Controllers;

/// <summary>
/// ApiClient配置控制器单元测试
/// Unit tests for ApiClientConfigController
/// </summary>
public class ApiClientConfigControllerTests
{
    private readonly Mock<ILogger<ApiClientConfigController>> _mockLogger;
    private readonly Mock<IPostCollectionConfigRepository> _mockPostCollectionRepo;
    private readonly Mock<IPostProcessingCenterConfigRepository> _mockPostProcessingRepo;
    private readonly Mock<IJushuitanErpConfigRepository> _mockJushuitanErpRepo;
    private readonly Mock<IWdtWmsConfigRepository> _mockWdtWmsRepo;
    private readonly Mock<IWdtErpFlagshipConfigRepository> _mockWdtErpFlagshipRepo;
    private readonly MockSystemClock _mockClock;
    private readonly IOptions<AppSettings> _appSettings;
    private readonly ApiClientConfigController _controller;

    public ApiClientConfigControllerTests()
    {
        _mockLogger = new Mock<ILogger<ApiClientConfigController>>();
        _mockPostCollectionRepo = new Mock<IPostCollectionConfigRepository>();
        _mockPostProcessingRepo = new Mock<IPostProcessingCenterConfigRepository>();
        _mockJushuitanErpRepo = new Mock<IJushuitanErpConfigRepository>();
        _mockWdtWmsRepo = new Mock<IWdtWmsConfigRepository>();
        _mockWdtErpFlagshipRepo = new Mock<IWdtErpFlagshipConfigRepository>();
        _mockClock = new MockSystemClock();
        _appSettings = Options.Create(new AppSettings());

        _controller = new ApiClientConfigController(
            _mockLogger.Object,
            new Mock<IServiceProvider>().Object,
            _appSettings,
            _mockClock,
            _mockPostCollectionRepo.Object,
            _mockPostProcessingRepo.Object,
            _mockJushuitanErpRepo.Object,
            _mockWdtWmsRepo.Object,
            _mockWdtErpFlagshipRepo.Object
        );

        // Setup HttpContext for IP address retrieval in audit logs
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region UpdateJushuitanErpConfig Tests

    [Fact]
    public async Task UpdateJushuitanErpConfig_WhenConfigDoesNotExist_CreatesNewConfig()
    {
        // Arrange
        var request = new JushuitanErpConfigRequest
        {
            Name = "聚水潭ERP配置",
            Url = "https://api.test.com",
            TimeoutMs = 5000,
            AppKey = "test_key",
            AppSecret = "test_secret",
            AccessToken = "test_token",
            Version = 2,
            IsUploadWeight = true,
            Type = 1,
            IsUnLid = false,
            Channel = "测试",
            DefaultWeight = -1,
            IsEnabled = true,
            Description = "测试配置"
        };

        _mockJushuitanErpRepo.Setup(r => r.GetByIdAsync(JushuitanErpConfig.SingletonId))
            .ReturnsAsync((JushuitanErpConfig?)null);
        _mockJushuitanErpRepo.Setup(r => r.UpsertAsync(It.IsAny<JushuitanErpConfig>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateJushuitanErpConfig(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("配置保存成功", response.Data);

        _mockJushuitanErpRepo.Verify(r => r.UpsertAsync(It.Is<JushuitanErpConfig>(c =>
            c.ConfigId == JushuitanErpConfig.SingletonId &&
            c.Name == request.Name &&
            c.Url == request.Url &&
            c.CreatedAt == _mockClock.LocalNow &&
            c.UpdatedAt == _mockClock.LocalNow
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateJushuitanErpConfig_WhenConfigExists_UpdatesExistingConfig()
    {
        // Arrange
        var existingCreatedAt = _mockClock.LocalNow.AddDays(-10);
        var existingConfig = new JushuitanErpConfig
        {
            ConfigId = JushuitanErpConfig.SingletonId,
            Name = "旧配置",
            Url = "https://old-api.test.com",
            TimeoutMs = 3000,
            AppKey = "old_key",
            AppSecret = "old_secret",
            AccessToken = "old_token",
            Version = 1,
            IsUploadWeight = false,
            Type = 0,
            IsUnLid = false,
            Channel = string.Empty,
            DefaultWeight = -1,
            IsEnabled = false,
            Description = "旧配置",
            CreatedAt = existingCreatedAt,
            UpdatedAt = existingCreatedAt
        };

        var request = new JushuitanErpConfigRequest
        {
            Name = "更新后的配置",
            Url = "https://new-api.test.com",
            TimeoutMs = 5000,
            AppKey = "new_key",
            AppSecret = "new_secret",
            AccessToken = "new_token",
            Version = 2,
            IsUploadWeight = true,
            Type = 1,
            IsUnLid = false,
            Channel = "新渠道",
            DefaultWeight = -1,
            IsEnabled = true,
            Description = "更新后的配置"
        };

        _mockJushuitanErpRepo.Setup(r => r.GetByIdAsync(JushuitanErpConfig.SingletonId))
            .ReturnsAsync(existingConfig);
        _mockJushuitanErpRepo.Setup(r => r.UpsertAsync(It.IsAny<JushuitanErpConfig>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateJushuitanErpConfig(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("配置保存成功", response.Data);

        _mockJushuitanErpRepo.Verify(r => r.UpsertAsync(It.Is<JushuitanErpConfig>(c =>
            c.ConfigId == JushuitanErpConfig.SingletonId &&
            c.Name == request.Name &&
            c.Url == request.Url &&
            c.CreatedAt == existingCreatedAt && // CreatedAt should be preserved
            c.UpdatedAt == _mockClock.LocalNow // UpdatedAt should be current time
        )), Times.Once);
    }

    [Fact]
    public async Task UpdateJushuitanErpConfig_WhenUpsertFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = new JushuitanErpConfigRequest
        {
            Name = "测试配置",
            Url = "https://api.test.com",
            TimeoutMs = 5000,
            AppKey = "test_key",
            AppSecret = "test_secret",
            AccessToken = "test_token",
            Version = 2,
            IsUploadWeight = true,
            Type = 1,
            IsUnLid = false,
            Channel = string.Empty,
            DefaultWeight = -1,
            IsEnabled = true,
            Description = null
        };

        _mockJushuitanErpRepo.Setup(r => r.GetByIdAsync(JushuitanErpConfig.SingletonId))
            .ReturnsAsync((JushuitanErpConfig?)null);
        _mockJushuitanErpRepo.Setup(r => r.UpsertAsync(It.IsAny<JushuitanErpConfig>()))
            .ReturnsAsync(false); // Simulate failure

        // Act
        var result = await _controller.UpdateJushuitanErpConfig(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var response = Assert.IsType<ApiResponse<string>>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Equal("保存配置失败", response.ErrorMessage);
        Assert.Equal("SAVE_FAILED", response.ErrorCode);
    }

    [Fact]
    public async Task UpdateJushuitanErpConfig_WhenExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var request = new JushuitanErpConfigRequest
        {
            Name = "测试配置",
            Url = "https://api.test.com",
            TimeoutMs = 5000,
            AppKey = "test_key",
            AppSecret = "test_secret",
            AccessToken = "test_token",
            Version = 2,
            IsUploadWeight = true,
            Type = 1,
            IsUnLid = false,
            Channel = string.Empty,
            DefaultWeight = -1,
            IsEnabled = true,
            Description = null
        };

        _mockJushuitanErpRepo.Setup(r => r.GetByIdAsync(JushuitanErpConfig.SingletonId))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.UpdateJushuitanErpConfig(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var response = Assert.IsType<ApiResponse<string>>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Contains("更新聚水潭ERP配置时发生错误", response.ErrorMessage);
    }

    #endregion

    #region UpdatePostCollectionConfig Tests

    [Fact]
    public async Task UpdatePostCollectionConfig_WhenConfigDoesNotExist_CreatesNewConfig()
    {
        // Arrange
        var request = new PostCollectionConfigRequest
        {
            Name = "邮政分揽投机构配置",
            Url = "https://post.test.com",
            WorkshopCode = "WS001",
            DeviceId = "DEV001",
            CompanyName = "测试公司",
            DeviceBarcode = "BAR001",
            OrganizationNumber = "ORG001",
            EmployeeNumber = "EMP001",
            TimeoutMs = 5000,
            IsEnabled = true,
            Description = "测试配置"
        };

        _mockPostCollectionRepo.Setup(r => r.GetByIdAsync(PostCollectionConfig.SingletonId))
            .ReturnsAsync((PostCollectionConfig?)null);
        _mockPostCollectionRepo.Setup(r => r.UpsertAsync(It.IsAny<PostCollectionConfig>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdatePostCollectionConfig(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("配置保存成功", response.Data);

        _mockPostCollectionRepo.Verify(r => r.UpsertAsync(It.Is<PostCollectionConfig>(c =>
            c.ConfigId == PostCollectionConfig.SingletonId &&
            c.Name == request.Name &&
            c.Url == request.Url &&
            c.WorkshopCode == request.WorkshopCode &&
            c.CreatedAt == _mockClock.LocalNow
        )), Times.Once);
    }

    [Fact]
    public async Task UpdatePostCollectionConfig_PreservesCreatedAtOnUpdate()
    {
        // Arrange
        var existingCreatedAt = _mockClock.LocalNow.AddMonths(-1);
        var existingConfig = new PostCollectionConfig
        {
            ConfigId = PostCollectionConfig.SingletonId,
            Name = "旧配置",
            Url = "https://old.test.com",
            WorkshopCode = "OLD",
            DeviceId = "OLD",
            CompanyName = "旧公司",
            DeviceBarcode = "OLD",
            OrganizationNumber = "OLD",
            EmployeeNumber = "OLD",
            TimeoutMs = 3000,
            IsEnabled = false,
            Description = "旧配置",
            CreatedAt = existingCreatedAt,
            UpdatedAt = existingCreatedAt
        };

        var request = new PostCollectionConfigRequest
        {
            Name = "新配置",
            Url = "https://new.test.com",
            WorkshopCode = "NEW",
            DeviceId = "NEW",
            CompanyName = "新公司",
            DeviceBarcode = "NEW",
            OrganizationNumber = "NEW",
            EmployeeNumber = "NEW",
            TimeoutMs = 5000,
            IsEnabled = true,
            Description = "新配置"
        };

        _mockPostCollectionRepo.Setup(r => r.GetByIdAsync(PostCollectionConfig.SingletonId))
            .ReturnsAsync(existingConfig);
        _mockPostCollectionRepo.Setup(r => r.UpsertAsync(It.IsAny<PostCollectionConfig>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdatePostCollectionConfig(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult);

        _mockPostCollectionRepo.Verify(r => r.UpsertAsync(It.Is<PostCollectionConfig>(c =>
            c.CreatedAt == existingCreatedAt && // Must preserve original CreatedAt
            c.UpdatedAt == _mockClock.LocalNow // Must update UpdatedAt
        )), Times.Once);
    }

    #endregion

    #region GetJushuitanErpConfig Tests

    [Fact]
    public async Task GetJushuitanErpConfig_WhenConfigExists_ReturnsConfig()
    {
        // Arrange
        var config = new JushuitanErpConfig
        {
            ConfigId = JushuitanErpConfig.SingletonId,
            Name = "测试配置",
            Url = "https://api.test.com",
            TimeoutMs = 5000,
            AppKey = "test_key_12345",
            AppSecret = "test_secret_12345",
            AccessToken = "test_token_12345",
            Version = 2,
            IsUploadWeight = true,
            Type = 1,
            IsUnLid = false,
            Channel = "测试",
            DefaultWeight = -1,
            IsEnabled = true,
            Description = "测试配置",
            CreatedAt = _mockClock.LocalNow,
            UpdatedAt = _mockClock.LocalNow
        };

        _mockJushuitanErpRepo.Setup(r => r.GetByIdAsync(JushuitanErpConfig.SingletonId))
            .ReturnsAsync(config);

        // Act
        var result = await _controller.GetJushuitanErpConfig();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<JushuitanErpConfigRequest>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal("测试配置", response.Data.Name);
        
        // Verify secrets are masked
        Assert.Equal("tes***345", response.Data.AppKey);
        Assert.Equal("tes***345", response.Data.AppSecret);
        Assert.Equal("tes***345", response.Data.AccessToken);
    }

    [Fact]
    public async Task GetJushuitanErpConfig_WhenConfigDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _mockJushuitanErpRepo.Setup(r => r.GetByIdAsync(JushuitanErpConfig.SingletonId))
            .ReturnsAsync((JushuitanErpConfig?)null);

        // Act
        var result = await _controller.GetJushuitanErpConfig();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<JushuitanErpConfigRequest>>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal("CONFIG_NOT_FOUND", response.ErrorCode);
    }

    #endregion
}
