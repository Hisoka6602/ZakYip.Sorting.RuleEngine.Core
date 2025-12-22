using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.EventHandlers;
using ZakYip.Sorting.RuleEngine.Application.Interfaces;
using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Domain.Events;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.EventHandlers;

/// <summary>
/// 格口落格完成事件处理器测试
/// Chute landing completed event handler tests
/// </summary>
public class ChuteLandingCompletedEventHandlerTests
{
    private readonly Mock<ILogger<ChuteLandingCompletedEventHandler>> _mockLogger;
    private readonly Mock<IWcsApiAdapterFactory> _mockApiFactory;
    private readonly Mock<IWcsApiAdapter> _mockAdapter;
    private readonly Mock<ILogRepository> _mockLogRepository;
    private readonly Mock<IParcelInfoRepository> _mockParcelRepository;
    private readonly Mock<IParcelLifecycleNodeRepository> _mockLifecycleRepository;
    private readonly ParcelCacheService _cacheService;
    private readonly MockSystemClock _clock;
    private readonly ChuteLandingCompletedEventHandler _handler;

    public ChuteLandingCompletedEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<ChuteLandingCompletedEventHandler>>();
        _mockApiFactory = new Mock<IWcsApiAdapterFactory>();
        _mockAdapter = new Mock<IWcsApiAdapter>();
        _mockApiFactory.Setup(f => f.GetActiveAdapter()).Returns(_mockAdapter.Object);
        _mockLogRepository = new Mock<ILogRepository>();
        _mockParcelRepository = new Mock<IParcelInfoRepository>();
        _mockLifecycleRepository = new Mock<IParcelLifecycleNodeRepository>();
        
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheLogger = new Mock<ILogger<ParcelCacheService>>();
        _cacheService = new ParcelCacheService(memoryCache, cacheLogger.Object);
        _clock = new MockSystemClock();
        
        _handler = new ChuteLandingCompletedEventHandler(
            _mockLogger.Object,
            _mockApiFactory.Object,
            _mockLogRepository.Object,
            _mockParcelRepository.Object,
            _mockLifecycleRepository.Object,
            _cacheService,
            _clock);
            
        // Setup default returns
        _mockParcelRepository.Setup(x => x.UpdateAsync(It.IsAny<ParcelInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockLifecycleRepository.Setup(x => x.AddAsync(It.IsAny<ParcelLifecycleNodeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_ValidEvent_UpdatesParcelAsCompleted()
    {
        // Arrange
        var parcel = new ParcelInfo
        {
            ParcelId = "TEST001",
            CartNumber = "CART001",
            TargetChute = "CH01",
            Status = ParcelStatus.Processing,
            LifecycleStage = ParcelLifecycleStage.ChuteAssigned
        };

        await _cacheService.SetAsync(parcel, CancellationToken.None);

        var landingEvent = new ChuteLandingCompletedEvent
        {
            ParcelId = "TEST001",
            ActualChute = "CH01",
            LandedAt = _clock.LocalNow
        };

        // Act
        await _handler.Handle(landingEvent, CancellationToken.None);

        // Assert
        Assert.Equal("CH01", parcel.ActualChute);
        Assert.Equal(ParcelStatus.Completed, parcel.Status);
        Assert.Equal(ParcelLifecycleStage.Landed, parcel.LifecycleStage);
        Assert.NotNull(parcel.CompletedAt);

        _mockParcelRepository.Verify(x => x.UpdateAsync(It.IsAny<ParcelInfo>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockLifecycleRepository.Verify(x => x.AddAsync(
            It.Is<ParcelLifecycleNodeEntity>(n => n.Stage == ParcelLifecycleStage.Landed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ParcelNotInCache_LoadsFromRepository()
    {
        // Arrange
        var parcel = new ParcelInfo
        {
            ParcelId = "TEST002",
            CartNumber = "CART002",
            Status = ParcelStatus.Processing
        };

        _mockParcelRepository.Setup(x => x.GetByIdAsync("TEST002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(parcel);

        var landingEvent = new ChuteLandingCompletedEvent
        {
            ParcelId = "TEST002",
            ActualChute = "CH02",
            LandedAt = _clock.LocalNow
        };

        // Act
        await _handler.Handle(landingEvent, CancellationToken.None);

        // Assert
        _mockParcelRepository.Verify(x => x.GetByIdAsync("TEST002", It.IsAny<CancellationToken>()), Times.Once);
        _mockParcelRepository.Verify(x => x.UpdateAsync(It.IsAny<ParcelInfo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentParcel_LogsWarning()
    {
        // Arrange
        _mockParcelRepository.Setup(x => x.GetByIdAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParcelInfo?)null);

        var landingEvent = new ChuteLandingCompletedEvent
        {
            ParcelId = "NONEXISTENT",
            ActualChute = "CH99",
            LandedAt = _clock.LocalNow
        };

        // Act
        await _handler.Handle(landingEvent, CancellationToken.None);

        // Assert
        _mockParcelRepository.Verify(x => x.UpdateAsync(It.IsAny<ParcelInfo>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockLifecycleRepository.Verify(x => x.AddAsync(It.IsAny<ParcelLifecycleNodeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
