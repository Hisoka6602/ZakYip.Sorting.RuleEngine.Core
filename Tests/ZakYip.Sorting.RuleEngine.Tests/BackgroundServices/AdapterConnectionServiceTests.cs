using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

namespace ZakYip.Sorting.RuleEngine.Tests.BackgroundServices;

/// <summary>
/// 适配器连接服务测试
/// Adapter connection service tests
/// </summary>
public class AdapterConnectionServiceTests
{
    private readonly Mock<IDwsAdapter> _dwsAdapterMock;
    private readonly Mock<IDownstreamCommunication> _downstreamCommunicationMock;
    private readonly Mock<IDwsConfigRepository> _dwsConfigRepositoryMock;
    private readonly Mock<ISorterConfigRepository> _sorterConfigRepositoryMock;
    private readonly Mock<ILogger<AdapterConnectionService>> _loggerMock;
    private readonly IServiceProvider _serviceProvider;

    public AdapterConnectionServiceTests()
    {
        _dwsAdapterMock = new Mock<IDwsAdapter>();
        _downstreamCommunicationMock = new Mock<IDownstreamCommunication>();
        _dwsConfigRepositoryMock = new Mock<IDwsConfigRepository>();
        _sorterConfigRepositoryMock = new Mock<ISorterConfigRepository>();
        _loggerMock = new Mock<ILogger<AdapterConnectionService>>();

        // 设置 DWS Adapter 属性
        _dwsAdapterMock.Setup(x => x.AdapterName).Returns("TestDwsAdapter");
        _dwsAdapterMock.Setup(x => x.ProtocolType).Returns("TCP");

        // 设置 ServiceProvider
        var services = new ServiceCollection();
        services.AddScoped(_ => _dwsConfigRepositoryMock.Object);
        services.AddScoped(_ => _sorterConfigRepositoryMock.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// 测试：DWS配置已启用时，应该启动DWS适配器
    /// Test: Should start DWS adapter when configuration is enabled
    /// </summary>
    [Fact]
    public async Task StartAsync_DwsConfigEnabled_ShouldStartDwsAdapter()
    {
        // Arrange
        var dwsConfig = new DwsConfig
        {
            ConfigId = DwsConfig.SingletonId,
            Mode = "Server",
            Host = "127.0.0.1",
            Port = 2001,
            DataTemplateId = 1,
            IsEnabled = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };

        _dwsConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(DwsConfig.SingletonId))
            .ReturnsAsync(dwsConfig);

        _sorterConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync((SorterConfig?)null);

        var service = new AdapterConnectionService(
            _serviceProvider,
            _dwsAdapterMock.Object,
            _downstreamCommunicationMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        _dwsAdapterMock.Verify(
            x => x.StartAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 测试：DWS配置已禁用时，不应该启动DWS适配器
    /// Test: Should not start DWS adapter when configuration is disabled
    /// </summary>
    [Fact]
    public async Task StartAsync_DwsConfigDisabled_ShouldNotStartDwsAdapter()
    {
        // Arrange
        var dwsConfig = new DwsConfig
        {
            ConfigId = DwsConfig.SingletonId,
            Mode = "Server",
            Host = "127.0.0.1",
            Port = 2001,
            DataTemplateId = 1,
            IsEnabled = false,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };

        _dwsConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(DwsConfig.SingletonId))
            .ReturnsAsync(dwsConfig);

        _sorterConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync((SorterConfig?)null);

        var service = new AdapterConnectionService(
            _serviceProvider,
            _dwsAdapterMock.Object,
            _downstreamCommunicationMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        _dwsAdapterMock.Verify(
            x => x.StartAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 测试：DWS适配器为null时，不应该启动
    /// Test: Should not start when DWS adapter is null
    /// </summary>
    [Fact]
    public async Task StartAsync_DwsAdapterNull_ShouldNotStart()
    {
        // Arrange
        var dwsConfig = new DwsConfig
        {
            ConfigId = DwsConfig.SingletonId,
            Mode = "Server",
            Host = "127.0.0.1",
            Port = 2001,
            DataTemplateId = 1,
            IsEnabled = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };

        _dwsConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(DwsConfig.SingletonId))
            .ReturnsAsync(dwsConfig);

        _sorterConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync((SorterConfig?)null);

        var service = new AdapterConnectionService(
            _serviceProvider,
            null, // DWS adapter is null
            _downstreamCommunicationMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert: 不应该抛出异常
        // Should not throw exception
    }

    /// <summary>
    /// 测试：分拣机配置已启用时，应该连接分拣机
    /// Test: Should connect Sorter when configuration is enabled
    /// </summary>
    [Fact]
    public async Task StartAsync_SorterConfigEnabled_ShouldConnectSorter()
    {
        // Arrange
        var sorterConfig = new SorterConfig
        {
            ConfigId = SorterConfig.SingletonId,
            Protocol = "TCP",
            Host = "127.0.0.1",
            Port = 3001,
            IsEnabled = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };

        _dwsConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(DwsConfig.SingletonId))
            .ReturnsAsync((DwsConfig?)null);

        _sorterConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync(sorterConfig);

        // 设置 downstream communication mock 在调用 StartAsync 后返回 true
        // Setup downstream communication mock to return true for IsEnabled after StartAsync is called
        _downstreamCommunicationMock
            .Setup(x => x.IsEnabled)
            .Returns(true);

        var service = new AdapterConnectionService(
            _serviceProvider,
            _dwsAdapterMock.Object,
            _downstreamCommunicationMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        _downstreamCommunicationMock.Verify(
            x => x.StartAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 测试：分拣机配置已禁用时，仍然会调用StartAsync加载配置，但连接不会启动
    /// Test: Should still call StartAsync to load config when Sorter configuration is disabled, but connection won't start
    /// </summary>
    [Fact]
    public async Task StartAsync_SorterConfigDisabled_ShouldCallStartAsyncButNotConnect()
    {
        // Arrange
        var sorterConfig = new SorterConfig
        {
            ConfigId = SorterConfig.SingletonId,
            Protocol = "TCP",
            Host = "127.0.0.1",
            Port = 3001,
            IsEnabled = false,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local)
        };

        _dwsConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(DwsConfig.SingletonId))
            .ReturnsAsync((DwsConfig?)null);

        _sorterConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync(sorterConfig);

        // 设置 downstream communication mock 返回 false（配置禁用）
        // Setup downstream communication mock to return false (config disabled)
        _downstreamCommunicationMock
            .Setup(x => x.IsEnabled)
            .Returns(false);

        var service = new AdapterConnectionService(
            _serviceProvider,
            _dwsAdapterMock.Object,
            _downstreamCommunicationMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        // StartAsync 应该被调用以触发配置加载（即使配置禁用）
        // StartAsync should be called to trigger config loading (even if config is disabled)
        _downstreamCommunicationMock.Verify(
            x => x.StartAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
