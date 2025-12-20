using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.EventHandlers;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.EventHandlers;

/// <summary>
/// 包裹创建事件处理器测试
/// Tests for ParcelCreatedEventHandler
/// </summary>
public class ParcelCreatedEventHandlerTests
{
    private readonly Mock<ILogger<ParcelCreatedEventHandler>> _mockLogger;
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<IParcelInfoRepository> _mockParcelRepository;
    private readonly Mock<IParcelLifecycleNodeRepository> _mockLifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly ISystemClock _clock;
    private readonly ParcelCreatedEventHandler _handler;

    public ParcelCreatedEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<ParcelCreatedEventHandler>>();
        _mockLogRepository = new Mock<ILogRepository>();
        _mockParcelRepository = new Mock<IParcelInfoRepository>();
        _mockLifecycleRepository = new Mock<IParcelLifecycleNodeRepository>();
        
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheLogger = new Mock<ILogger<ParcelCacheService>>();
        _cacheService = new ParcelCacheService(memoryCache, cacheLogger.Object);
        _clock = new SystemClock();
        
        _handler = new ParcelCreatedEventHandler(
            _mockLogger.Object,
            _mockLogRepository.Object,
            _mockParcelRepository.Object,
            _mockLifecycleRepository.Object,
            _cacheService,
            _clock);
            
        // Setup默认返回值
        _mockParcelRepository.Setup(x => x.AddAsync(It.IsAny<ZakYip.Sorting.RuleEngine.Domain.Entities.ParcelInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockLifecycleRepository.Setup(x => x.AddAsync(It.IsAny<ZakYip.Sorting.RuleEngine.Domain.Entities.ParcelLifecycleNodeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_ParcelCreatedEvent_ShouldLogInformation()
    {
        // Arrange
        var parcelCreatedEvent = new ZakYip.Sorting.RuleEngine.Domain.Events.ParcelCreatedEvent
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Barcode = "1234567890",
            SequenceNumber = 1
        };

        // Act
        await _handler.Handle(parcelCreatedEvent, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            x => x.LogInfoAsync(
                It.Is<string>(s => s.Contains("PKG001")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ParcelCreatedEvent_ShouldIncludeSequenceNumber()
    {
        // Arrange
        var parcelCreatedEvent = new ZakYip.Sorting.RuleEngine.Domain.Events.ParcelCreatedEvent
        {
            ParcelId = "PKG002",
            CartNumber = "CART002",
            SequenceNumber = 42
        };

        // Act
        await _handler.Handle(parcelCreatedEvent, CancellationToken.None);

        // Assert
        _mockLogRepository.Verify(
            x => x.LogInfoAsync(
                It.IsAny<string>(),
                It.Is<string>(s => s.Contains("42")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
