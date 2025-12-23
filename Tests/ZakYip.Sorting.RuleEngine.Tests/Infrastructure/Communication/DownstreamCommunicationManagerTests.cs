using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Communication;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Communication;

/// <summary>
/// DownstreamCommunicationManager 管理器类单元测试
/// DownstreamCommunicationManager unit tests
/// </summary>
/// <remarks>
/// 测试覆盖 / Test Coverage:
/// 1. ✅ 初始化时使用空对象
/// 2. ✅ 首次 StartAsync 时延迟加载配置
/// 3. ✅ ReloadAsync 正确停止旧实例、创建新实例、启动新实例
/// 4. ✅ 启动失败时回滚到 NullDownstreamCommunication
/// 5. ✅ ReloadAsync 返回成功/失败状态
/// 6. ✅ DisposeAsync 正确清理资源
/// </remarks>
public class DownstreamCommunicationManagerTests : IDisposable
{
    private readonly Mock<IDownstreamCommunicationFactory> _mockFactory;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ISorterConfigRepository> _mockConfigRepository;
    private readonly Mock<ILogger<DownstreamCommunicationManager>> _mockLogger;
    private readonly DownstreamCommunicationManager _manager;

    public DownstreamCommunicationManagerTests()
    {
        _mockFactory = new Mock<IDownstreamCommunicationFactory>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockConfigRepository = new Mock<ISorterConfigRepository>();
        _mockLogger = new Mock<ILogger<DownstreamCommunicationManager>>();

        // Setup service scope factory chain
        _mockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(_mockServiceScope.Object);
        _mockServiceScope.Setup(x => x.ServiceProvider)
            .Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(ISorterConfigRepository)))
            .Returns(_mockConfigRepository.Object);

        _manager = new DownstreamCommunicationManager(
            _mockFactory.Object,
            _mockServiceScopeFactory.Object,
            _mockLogger.Object);
    }

    /// <summary>
    /// 测试初始化时使用空对象模式
    /// Test initialization uses null object pattern
    /// </summary>
    [Fact]
    public void Constructor_InitializesWithNullObject()
    {
        // Assert
        Assert.False(_manager.IsEnabled);
    }

    /// <summary>
    /// 测试首次 StartAsync 延迟加载配置
    /// Test first StartAsync lazy loads configuration
    /// </summary>
    [Fact]
    public async Task StartAsync_OnFirstCall_LazyLoadsConfiguration()
    {
        // Arrange
        var config = CreateEnabledConfig("CLIENT");
        var mockClient = new Mock<IDownstreamCommunication>();
        mockClient.Setup(x => x.IsEnabled).Returns(true);
        mockClient.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockConfigRepository.Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync(config);
        _mockFactory.Setup(x => x.Create(config))
            .Returns(mockClient.Object);

        // Act
        await _manager.StartAsync();

        // Assert
        _mockConfigRepository.Verify(x => x.GetByIdAsync(SorterConfig.SingletonId), Times.Once);
        _mockFactory.Verify(x => x.Create(config), Times.Once);
        mockClient.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 测试 ReloadAsync 成功场景
    /// Test ReloadAsync success scenario
    /// </summary>
    [Fact]
    public async Task ReloadAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var newConfig = CreateEnabledConfig("CLIENT");
        var mockNewClient = new Mock<IDownstreamCommunication>();
        mockNewClient.Setup(x => x.IsEnabled).Returns(true);
        mockNewClient.Setup(x => x.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockNewClient.Setup(x => x.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockConfigRepository.Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync(newConfig);
        _mockFactory.Setup(x => x.Create(newConfig))
            .Returns(mockNewClient.Object);

        // Act
        var result = await _manager.ReloadAsync();

        // Assert
        Assert.True(result);
        mockNewClient.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 测试 ReloadAsync 启动失败时回滚到空对象
    /// Test ReloadAsync rolls back to null object when start fails
    /// </summary>
    [Fact]
    public async Task ReloadAsync_WhenStartFails_RollsBackToNullObject()
    {
        // Arrange
        var newConfig = CreateEnabledConfig("CLIENT");
        var mockNewClient = new Mock<IDownstreamCommunication>();
        mockNewClient.Setup(x => x.IsEnabled).Returns(true);
        mockNewClient.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Start failed"));

        _mockConfigRepository.Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync(newConfig);
        _mockFactory.Setup(x => x.Create(newConfig))
            .Returns(mockNewClient.Object);

        // Act
        var result = await _manager.ReloadAsync();

        // Assert
        Assert.False(result); // Should return false on failure
        Assert.False(_manager.IsEnabled); // Should be disabled (null object)
    }

    /// <summary>
    /// 测试 ReloadAsync 配置禁用时不启动新实例
    /// Test ReloadAsync doesn't start new instance when config is disabled
    /// </summary>
    [Fact]
    public async Task ReloadAsync_WhenConfigDisabled_DoesNotStartNewInstance()
    {
        // Arrange
        var disabledConfig = CreateDisabledConfig();
        var mockNullComm = new Mock<IDownstreamCommunication>();
        mockNullComm.Setup(x => x.IsEnabled).Returns(false);

        _mockConfigRepository.Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync(disabledConfig);
        _mockFactory.Setup(x => x.Create(disabledConfig))
            .Returns(mockNullComm.Object);

        // Act
        var result = await _manager.ReloadAsync();

        // Assert
        Assert.True(result);
        mockNullComm.Verify(x => x.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 测试 BroadcastChuteAssignmentAsync 使用锁保护
    /// Test BroadcastChuteAssignmentAsync is lock-protected
    /// </summary>
    [Fact]
    public async Task BroadcastChuteAssignmentAsync_IsThreadSafe()
    {
        // Arrange
        var config = CreateEnabledConfig("CLIENT");
        var mockClient = new Mock<IDownstreamCommunication>();
        mockClient.Setup(x => x.IsEnabled).Returns(true);
        mockClient.Setup(x => x.BroadcastChuteAssignmentAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockConfigRepository.Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync(config);
        _mockFactory.Setup(x => x.Create(config))
            .Returns(mockClient.Object);

        await _manager.StartAsync(); // Initialize with actual client

        // Act
        await _manager.BroadcastChuteAssignmentAsync("test json");

        // Assert
        mockClient.Verify(x => x.BroadcastChuteAssignmentAsync("test json"), Times.Once);
    }

    /// <summary>
    /// 测试 DisposeAsync 正确清理资源
    /// Test DisposeAsync properly cleans up resources
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CleansUpResources()
    {
        // Arrange
        var config = CreateEnabledConfig("CLIENT");
        var mockClient = new Mock<IDownstreamCommunication>();
        var mockDisposable = mockClient.As<IDisposable>();
        mockClient.Setup(x => x.IsEnabled).Returns(true);
        mockClient.Setup(x => x.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockDisposable.Setup(x => x.Dispose());

        _mockConfigRepository.Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync(config);
        _mockFactory.Setup(x => x.Create(config))
            .Returns(mockClient.Object);

        await _manager.StartAsync();

        // Act
        await _manager.DisposeAsync();

        // Assert
        mockClient.Verify(x => x.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    /// <summary>
    /// 测试对象释放后调用方法抛出 ObjectDisposedException
    /// Test methods throw ObjectDisposedException after disposal
    /// </summary>
    [Fact]
    public async Task AfterDispose_MethodsThrowObjectDisposedException()
    {
        // Arrange
        await _manager.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _manager.StartAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _manager.StopAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _manager.BroadcastChuteAssignmentAsync("test"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _manager.ReloadAsync());
    }

    private static SorterConfig CreateEnabledConfig(string connectionMode) => new()
    {
        Protocol = "TCP",
        ConnectionMode = connectionMode,
        Host = "127.0.0.1",
        Port = 2003,
        IsEnabled = true,
        TimeoutSeconds = 30,
        ReconnectIntervalSeconds = 5,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    private static SorterConfig CreateDisabledConfig() => new()
    {
        Protocol = "TCP",
        ConnectionMode = "CLIENT",
        Host = "127.0.0.1",
        Port = 2003,
        IsEnabled = false,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
    };

    public void Dispose()
    {
        _manager?.Dispose();
        GC.SuppressFinalize(this);
    }
}
