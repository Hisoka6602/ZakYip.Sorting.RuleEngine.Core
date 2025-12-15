using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// DWS超时处理测试 / DWS timeout handling tests
/// </summary>
public class DwsTimeoutHandlingTests
{
    private readonly Mock<ILogger<ParcelOrchestrationService>> _mockLogger;
    private readonly Mock<IPublisher> _mockPublisher;
    private readonly Mock<IRuleEngineService> _mockRuleEngineService;
    private readonly Mock<ISystemClock> _mockClock;
    private readonly TestDwsTimeoutSettings _timeoutSettings;
    private readonly IMemoryCache _cache;
    private readonly IServiceProvider _serviceProvider;
    private DateTime _currentTime;

    public DwsTimeoutHandlingTests()
    {
        _mockLogger = new Mock<ILogger<ParcelOrchestrationService>>();
        _mockPublisher = new Mock<IPublisher>();
        _mockRuleEngineService = new Mock<IRuleEngineService>();
        _mockClock = new Mock<ISystemClock>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        
        _currentTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);
        _mockClock.SetupGet(c => c.LocalNow).Returns(() => _currentTime);
        
        // 设置默认配置
        _timeoutSettings = new TestDwsTimeoutSettings
        {
            Enabled = true,
            MinDwsWaitSeconds = 2,
            MaxDwsWaitSeconds = 30,
            ExceptionChuteId = 999,
            CheckIntervalSeconds = 5
        };
        
        // 创建 ServiceProvider 用于测试
        var services = new ServiceCollection();
        services.AddScoped(_ => _mockRuleEngineService.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    private ParcelOrchestrationService CreateService()
    {
        return new ParcelOrchestrationService(
            _mockLogger.Object,
            _mockPublisher.Object,
            _serviceProvider,
            _cache,
            _mockClock.Object,
            _timeoutSettings);
    }

    [Fact]
    public async Task ReceiveDwsDataAsync_WithinValidTimeWindow_ShouldAcceptData()
    {
        // Arrange
        var service = CreateService();
        var parcelId = "PKG001";
        var cartNumber = "CART001";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进5秒（在2秒到30秒的有效窗口内）
        _currentTime = _currentTime.AddSeconds(5);
        
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        // Act
        var result = await service.ReceiveDwsDataAsync(parcelId, dwsData);

        // Assert
        Assert.True(result, "应该接受在有效时间窗口内的DWS数据 / Should accept DWS data within valid time window");
    }

    [Fact]
    public async Task ReceiveDwsDataAsync_TooEarly_ShouldRejectData()
    {
        // Arrange
        var service = CreateService();
        var parcelId = "PKG002";
        var cartNumber = "CART002";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进1秒（小于最小等待时间2秒）
        _currentTime = _currentTime.AddSeconds(1);
        
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        // Act
        var result = await service.ReceiveDwsDataAsync(parcelId, dwsData);

        // Assert
        Assert.False(result, "应该拒绝过早接收的DWS数据（可能是上一个包裹） / Should reject DWS data received too early (possibly from previous parcel)");
    }

    [Fact]
    public async Task ReceiveDwsDataAsync_Timeout_ShouldRejectData()
    {
        // Arrange
        var service = CreateService();
        var parcelId = "PKG003";
        var cartNumber = "CART003";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进35秒（超过最大等待时间30秒）
        _currentTime = _currentTime.AddSeconds(35);
        
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        // Act
        var result = await service.ReceiveDwsDataAsync(parcelId, dwsData);

        // Assert
        Assert.False(result, "应该拒绝超时后接收的DWS数据 / Should reject DWS data received after timeout");
    }

    [Fact]
    public async Task ReceiveDwsDataAsync_AtMinimumBoundary_ShouldAcceptData()
    {
        // Arrange
        var service = CreateService();
        var parcelId = "PKG004";
        var cartNumber = "CART004";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进恰好2秒（最小等待时间边界）
        _currentTime = _currentTime.AddSeconds(2);
        
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        // Act
        var result = await service.ReceiveDwsDataAsync(parcelId, dwsData);

        // Assert
        Assert.True(result, "应该接受恰好达到最小等待时间的DWS数据 / Should accept DWS data at minimum wait time boundary");
    }

    [Fact]
    public async Task ReceiveDwsDataAsync_AtMaximumBoundary_ShouldAcceptData()
    {
        // Arrange
        var service = CreateService();
        var parcelId = "PKG005";
        var cartNumber = "CART005";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进恰好30秒（最大等待时间边界）
        _currentTime = _currentTime.AddSeconds(30);
        
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        // Act
        var result = await service.ReceiveDwsDataAsync(parcelId, dwsData);

        // Assert
        Assert.True(result, "应该接受恰好达到最大等待时间的DWS数据 / Should accept DWS data at maximum wait time boundary");
    }

    [Fact]
    public async Task ReceiveDwsDataAsync_WhenTimeoutCheckDisabled_ShouldAlwaysAcceptData()
    {
        // Arrange
        _timeoutSettings.Enabled = false; // 禁用超时检查
        
        var service = CreateService();
        var parcelId = "PKG006";
        var cartNumber = "CART006";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进1秒（正常情况下会被拒绝）
        _currentTime = _currentTime.AddSeconds(1);
        
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        // Act
        var result = await service.ReceiveDwsDataAsync(parcelId, dwsData);

        // Assert
        Assert.True(result, "禁用超时检查后应该接受任何时间的DWS数据 / Should accept DWS data at any time when timeout check is disabled");
    }

    [Fact]
    public async Task CheckTimeoutParcelsAsync_WithTimedOutParcel_ShouldDetectTimeout()
    {
        // Arrange
        var service = CreateService();
        var parcelId = "PKG007";
        var cartNumber = "CART007";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进35秒（超过最大等待时间30秒）
        _currentTime = _currentTime.AddSeconds(35);

        // Act
        await service.CheckTimeoutParcelsAsync();

        // Assert - 验证日志记录了超时检测
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("检测到超时包裹")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "应该记录检测到超时包裹的警告日志 / Should log warning about detected timed-out parcel");
    }

    [Fact]
    public async Task CheckTimeoutParcelsAsync_WithParcelInValidWindow_ShouldNotDetectTimeout()
    {
        // Arrange
        var service = CreateService();
        var parcelId = "PKG008";
        var cartNumber = "CART008";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进10秒（在有效窗口内）
        _currentTime = _currentTime.AddSeconds(10);

        // Act
        await service.CheckTimeoutParcelsAsync();

        // Assert - 验证没有记录超时检测日志
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("检测到超时包裹")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "不应该记录超时警告（包裹仍在有效窗口内）/ Should not log timeout warning (parcel still in valid window)");
    }

    [Fact]
    public async Task CheckTimeoutParcelsAsync_WithReceivedDwsData_ShouldNotDetectTimeout()
    {
        // Arrange
        var service = CreateService();
        var parcelId = "PKG009";
        var cartNumber = "CART009";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进5秒后接收DWS数据
        _currentTime = _currentTime.AddSeconds(5);
        var dwsData = new DwsData
        {
            Barcode = "1234567890",
            Weight = 1500,
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };
        await service.ReceiveDwsDataAsync(parcelId, dwsData);
        
        // 再前进40秒（总共45秒，超过超时时间）
        _currentTime = _currentTime.AddSeconds(40);

        // Act
        await service.CheckTimeoutParcelsAsync();

        // Assert - 验证没有记录超时检测日志（因为已经接收了DWS数据）
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("检测到超时包裹")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "已接收DWS数据的包裹不应被检测为超时 / Parcels with received DWS data should not be detected as timed out");
    }

    [Fact]
    public async Task CheckTimeoutParcelsAsync_WhenDisabled_ShouldNotCheckTimeout()
    {
        // Arrange
        _timeoutSettings.Enabled = false; // 禁用超时检查
        
        var service = CreateService();
        var parcelId = "PKG010";
        var cartNumber = "CART010";
        
        await service.CreateParcelAsync(parcelId, cartNumber);
        
        // 前进100秒（远超超时时间）
        _currentTime = _currentTime.AddSeconds(100);

        // Act
        await service.CheckTimeoutParcelsAsync();

        // Assert - 验证没有记录任何超时日志
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("检测到超时包裹")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "禁用超时检查后不应检测超时 / Should not detect timeout when timeout check is disabled");
    }
}

/// <summary>
/// 测试用的DWS超时配置实现 / Test implementation of DWS timeout settings
/// </summary>
internal class TestDwsTimeoutSettings : IDwsTimeoutSettings
{
    public bool Enabled { get; set; }
    public int MinDwsWaitSeconds { get; set; }
    public int MaxDwsWaitSeconds { get; set; }
    public long ExceptionChuteId { get; set; }
    public int CheckIntervalSeconds { get; set; }
}
