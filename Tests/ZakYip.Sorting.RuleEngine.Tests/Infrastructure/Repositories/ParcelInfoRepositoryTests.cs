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
/// 包裹信息仓储测试
/// Parcel info repository tests
/// </summary>
public class ParcelInfoRepositoryTests : IDisposable
{
    private readonly SqliteLogDbContext _context;
    private readonly SqliteParcelInfoRepository _repository;
    private readonly ILogger<SqliteParcelInfoRepository> _logger;
    private readonly MockSystemClock _clock;

    public ParcelInfoRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SqliteLogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SqliteLogDbContext(options);
        _logger = Mock.Of<ILogger<SqliteParcelInfoRepository>>();
        _clock = new MockSystemClock();
        _repository = new SqliteParcelInfoRepository(_context, _logger, _clock);
    }

    [Fact]
    public async Task AddAsync_ValidParcel_ReturnsTrue()
    {
        // Arrange
        var parcel = new ParcelInfo
        {
            ParcelId = "TEST001",
            CartNumber = "CART001",
            Status = ParcelStatus.Pending,
            LifecycleStage = ParcelLifecycleStage.Created
        };

        // Act
        var result = await _repository.AddAsync(parcel);

        // Assert
        Assert.True(result);
        var savedParcel = await _repository.GetByIdAsync("TEST001");
        Assert.NotNull(savedParcel);
        Assert.Equal("TEST001", savedParcel.ParcelId);
        Assert.Equal("CART001", savedParcel.CartNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingParcel_ReturnsParcel()
    {
        // Arrange
        var parcel = new ParcelInfo
        {
            ParcelId = "TEST002",
            CartNumber = "CART002",
            Status = ParcelStatus.Processing
        };
        await _repository.AddAsync(parcel);

        // Act
        var result = await _repository.GetByIdAsync("TEST002");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST002", result.ParcelId);
        Assert.Equal(ParcelStatus.Processing, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentParcel_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("NONEXISTENT");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ExistingParcel_ReturnsTrue()
    {
        // Arrange
        var parcel = new ParcelInfo
        {
            ParcelId = "TEST003",
            CartNumber = "CART003",
            Status = ParcelStatus.Pending
        };
        await _repository.AddAsync(parcel);

        parcel.Status = ParcelStatus.Completed;
        parcel.TargetChute = "CH01";

        // Act
        var result = await _repository.UpdateAsync(parcel);

        // Assert
        Assert.True(result);
        var updatedParcel = await _repository.GetByIdAsync("TEST003");
        Assert.NotNull(updatedParcel);
        Assert.Equal(ParcelStatus.Completed, updatedParcel.Status);
        Assert.Equal("CH01", updatedParcel.TargetChute);
    }

    [Fact]
    public async Task SearchAsync_ByStatus_ReturnsFilteredParcels()
    {
        // Arrange
        await _repository.AddAsync(new ParcelInfo { ParcelId = "P1", CartNumber = "C1", Status = ParcelStatus.Pending });
        await _repository.AddAsync(new ParcelInfo { ParcelId = "P2", CartNumber = "C2", Status = ParcelStatus.Completed });
        await _repository.AddAsync(new ParcelInfo { ParcelId = "P3", CartNumber = "C3", Status = ParcelStatus.Pending });

        // Act
        var (items, totalCount) = await _repository.SearchAsync(status: ParcelStatus.Pending);

        // Assert
        Assert.Equal(2, totalCount);
        Assert.Equal(2, items.Count);
        Assert.All(items, p => Assert.Equal(ParcelStatus.Pending, p.Status));
    }

    [Fact]
    public async Task SearchAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 10; i++)
        {
            await _repository.AddAsync(new ParcelInfo
            {
                ParcelId = $"P{i:D3}",
                CartNumber = $"C{i:D3}",
                Status = ParcelStatus.Pending
            });
        }

        // Act
        var (items, totalCount) = await _repository.SearchAsync(page: 2, pageSize: 3);

        // Assert
        Assert.Equal(10, totalCount);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task GetLatestWithoutDwsDataAsync_ReturnsLatestParcel()
    {
        // Arrange
        await _repository.AddAsync(new ParcelInfo 
        { 
            ParcelId = "P1", 
            CartNumber = "C1", 
            Weight = null,
            CreatedAt = _clock.LocalNow.AddMinutes(-10)
        });

        await _repository.AddAsync(new ParcelInfo 
        { 
            ParcelId = "P2", 
            CartNumber = "C2", 
            Weight = null,
            CreatedAt = _clock.LocalNow.AddMinutes(-5)
        });

        await _repository.AddAsync(new ParcelInfo 
        { 
            ParcelId = "P3", 
            CartNumber = "C3", 
            Weight = 1000,
            CreatedAt = _clock.LocalNow
        });

        // Act
        var result = await _repository.GetLatestWithoutDwsDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("P2", result.ParcelId);
    }

    [Fact]
    public async Task GetByBagIdAsync_ReturnsParcelsInBag()
    {
        // Arrange
        await _repository.AddAsync(new ParcelInfo { ParcelId = "P1", CartNumber = "C1", BagId = "BAG001" });
        await _repository.AddAsync(new ParcelInfo { ParcelId = "P2", CartNumber = "C2", BagId = "BAG001" });
        await _repository.AddAsync(new ParcelInfo { ParcelId = "P3", CartNumber = "C3", BagId = "BAG002" });

        // Act
        var result = await _repository.GetByBagIdAsync("BAG001");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal("BAG001", p.BagId));
    }

    [Fact]
    public async Task BatchUpdateAsync_UpdatesMultipleParcels()
    {
        // Arrange
        var p1 = new ParcelInfo { ParcelId = "P1", CartNumber = "C1", Status = ParcelStatus.Pending };
        var p2 = new ParcelInfo { ParcelId = "P2", CartNumber = "C2", Status = ParcelStatus.Pending };
        await _repository.AddAsync(p1);
        await _repository.AddAsync(p2);

        p1.Status = ParcelStatus.Completed;
        p2.Status = ParcelStatus.Completed;

        // Act
        var result = await _repository.BatchUpdateAsync(new[] { p1, p2 });

        // Assert
        Assert.Equal(2, result);
        var updated1 = await _repository.GetByIdAsync("P1");
        var updated2 = await _repository.GetByIdAsync("P2");
        Assert.Equal(ParcelStatus.Completed, updated1!.Status);
        Assert.Equal(ParcelStatus.Completed, updated2!.Status);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
