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
    private readonly Mock<ISorterAdapterManager> _sorterAdapterManagerMock;
    private readonly Mock<IDwsConfigRepository> _dwsConfigRepositoryMock;
    private readonly Mock<ISorterConfigRepository> _sorterConfigRepositoryMock;
    private readonly Mock<ILogger<AdapterConnectionService>> _loggerMock;
    private readonly IServiceProvider _serviceProvider;

    public AdapterConnectionServiceTests()
    {
        _dwsAdapterMock = new Mock<IDwsAdapter>();
        _sorterAdapterManagerMock = new Mock<ISorterAdapterManager>();
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
            _sorterAdapterManagerMock.Object,
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
            _sorterAdapterManagerMock.Object,
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
            _sorterAdapterManagerMock.Object,
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

        var service = new AdapterConnectionService(
            _serviceProvider,
            _dwsAdapterMock.Object,
            _sorterAdapterManagerMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        _sorterAdapterManagerMock.Verify(
            x => x.ConnectAsync(It.IsAny<SorterConfig>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
