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
    private readonly Mock<IDwsAdapterManager> _dwsAdapterManagerMock;
    private readonly Mock<ISorterAdapterManager> _sorterAdapterManagerMock;
    private readonly Mock<IDwsConfigRepository> _dwsConfigRepositoryMock;
    private readonly Mock<ISorterConfigRepository> _sorterConfigRepositoryMock;
    private readonly Mock<ILogger<AdapterConnectionService>> _loggerMock;
    private readonly IServiceProvider _serviceProvider;

    public AdapterConnectionServiceTests()
    {
        _dwsAdapterManagerMock = new Mock<IDwsAdapterManager>();
        _sorterAdapterManagerMock = new Mock<ISorterAdapterManager>();
        _dwsConfigRepositoryMock = new Mock<IDwsConfigRepository>();
        _sorterConfigRepositoryMock = new Mock<ISorterConfigRepository>();
        _loggerMock = new Mock<ILogger<AdapterConnectionService>>();

        // 设置 ServiceProvider
        var services = new ServiceCollection();
        services.AddScoped(_ => _dwsConfigRepositoryMock.Object);
        services.AddScoped(_ => _sorterConfigRepositoryMock.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// 测试：DWS配置已启用时，应该连接DWS
    /// Test: Should connect DWS when configuration is enabled
    /// </summary>
    [Fact]
    public async Task StartAsync_DwsConfigEnabled_ShouldConnectDws()
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
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _dwsConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(DwsConfig.SingletonId))
            .ReturnsAsync(dwsConfig);

        _sorterConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync((SorterConfig?)null);

        var service = new AdapterConnectionService(
            _serviceProvider,
            _dwsAdapterManagerMock.Object,
            _sorterAdapterManagerMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        _dwsAdapterManagerMock.Verify(
            x => x.ConnectAsync(It.IsAny<DwsConfig>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// 测试：DWS配置已禁用时，不应该连接DWS
    /// Test: Should not connect DWS when configuration is disabled
    /// </summary>
    [Fact]
    public async Task StartAsync_DwsConfigDisabled_ShouldNotConnectDws()
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
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _dwsConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(DwsConfig.SingletonId))
            .ReturnsAsync(dwsConfig);

        _sorterConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync((SorterConfig?)null);

        var service = new AdapterConnectionService(
            _serviceProvider,
            _dwsAdapterManagerMock.Object,
            _sorterAdapterManagerMock.Object,
            _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        _dwsAdapterManagerMock.Verify(
            x => x.ConnectAsync(It.IsAny<DwsConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 测试：连接失败时不应该抛出异常
    /// Test: Should not throw exception when connection fails
    /// </summary>
    [Fact]
    public async Task StartAsync_ConnectionFails_ShouldNotThrowException()
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
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _dwsConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(DwsConfig.SingletonId))
            .ReturnsAsync(dwsConfig);

        _sorterConfigRepositoryMock
            .Setup(x => x.GetByIdAsync(SorterConfig.SingletonId))
            .ReturnsAsync((SorterConfig?)null);

        _dwsAdapterManagerMock
            .Setup(x => x.ConnectAsync(It.IsAny<DwsConfig>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("连接失败"));

        var service = new AdapterConnectionService(
            _serviceProvider,
            _dwsAdapterManagerMock.Object,
            _sorterAdapterManagerMock.Object,
            _loggerMock.Object);

        // Act & Assert - 不应该抛出异常
        await service.StartAsync(CancellationToken.None).ConfigureAwait(false);

        // 验证异常被记录
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }
}
