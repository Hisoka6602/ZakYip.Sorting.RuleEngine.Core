using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Sqlite;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;
using ZakYip.Sorting.RuleEngine.Tests.Mocks;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Repositories;

/// <summary>
/// 包裹生命周期节点仓储测试
/// Parcel lifecycle node repository tests
/// </summary>
public class ParcelLifecycleNodeRepositoryTests : IDisposable
{
    private readonly SqliteLogDbContext _context;
    private readonly SqliteParcelLifecycleNodeRepository _repository;
    private readonly ILogger<SqliteParcelLifecycleNodeRepository> _logger;
    private readonly MockSystemClock _clock;

    public ParcelLifecycleNodeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SqliteLogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SqliteLogDbContext(options);
        _logger = Mock.Of<ILogger<SqliteParcelLifecycleNodeRepository>>();
        _clock = new MockSystemClock();
        _repository = new SqliteParcelLifecycleNodeRepository(_context, _logger, _clock);
    }

    [Fact]
    public async Task AddAsync_ValidNode_ReturnsTrue()
    {
        // Arrange
        var node = new ParcelLifecycleNodeEntity
        {
            ParcelId = "TEST001",
            Stage = ParcelLifecycleStage.Created,
            EventTime = DateTime.UtcNow,
            Description = "包裹创建"
        };

        // Act
        var result = await _repository.AddAsync(node);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetByParcelIdAsync_ReturnsNodesOrderedByTimeDesc()
    {
        // Arrange
        var parcelId = "TEST002";
        var now = DateTime.UtcNow;

        await _repository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = parcelId,
            Stage = ParcelLifecycleStage.Created,
            EventTime = now.AddMinutes(-10)
        });

        await _repository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = parcelId,
            Stage = ParcelLifecycleStage.DwsReceived,
            EventTime = now.AddMinutes(-5)
        });

        await _repository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = parcelId,
            Stage = ParcelLifecycleStage.Landed,
            EventTime = now
        });

        // Act
        var nodes = await _repository.GetByParcelIdAsync(parcelId);

        // Assert
        Assert.Equal(3, nodes.Count);
        Assert.Equal(ParcelLifecycleStage.Landed, nodes[0].Stage);
        Assert.Equal(ParcelLifecycleStage.DwsReceived, nodes[1].Stage);
        Assert.Equal(ParcelLifecycleStage.Created, nodes[2].Stage);
    }

    [Fact]
    public async Task BatchAddAsync_AddsMultipleNodes()
    {
        // Arrange
        var nodes = new[]
        {
            new ParcelLifecycleNodeEntity
            {
                ParcelId = "P1",
                Stage = ParcelLifecycleStage.Created,
                EventTime = DateTime.UtcNow
            },
            new ParcelLifecycleNodeEntity
            {
                ParcelId = "P1",
                Stage = ParcelLifecycleStage.DwsReceived,
                EventTime = DateTime.UtcNow.AddMinutes(1)
            }
        };

        // Act
        var result = await _repository.BatchAddAsync(nodes);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetByTimeRangeAsync_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;

        await _repository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = "P1",
            Stage = ParcelLifecycleStage.Created,
            EventTime = now.AddHours(-2)
        });

        await _repository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = "P2",
            Stage = ParcelLifecycleStage.Created,
            EventTime = now.AddHours(-1)
        });

        await _repository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = "P3",
            Stage = ParcelLifecycleStage.Created,
            EventTime = now
        });

        // Act
        var (items, totalCount) = await _repository.GetByTimeRangeAsync(
            startTime: now.AddHours(-1.5),
            endTime: now.AddMinutes(-30));

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal("P2", items[0].ParcelId);
    }

    [Fact]
    public async Task GetByTimeRangeAsync_WithStageFilter_FiltersCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;

        await _repository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = "P1",
            Stage = ParcelLifecycleStage.Created,
            EventTime = now
        });

        await _repository.AddAsync(new ParcelLifecycleNodeEntity
        {
            ParcelId = "P2",
            Stage = ParcelLifecycleStage.Landed,
            EventTime = now
        });

        // Act
        var (items, totalCount) = await _repository.GetByTimeRangeAsync(
            startTime: now.AddHours(-1),
            endTime: now.AddHours(1),
            stage: ParcelLifecycleStage.Landed);

        // Assert
        Assert.Equal(1, totalCount);
        Assert.Single(items);
        Assert.Equal(ParcelLifecycleStage.Landed, items[0].Stage);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
