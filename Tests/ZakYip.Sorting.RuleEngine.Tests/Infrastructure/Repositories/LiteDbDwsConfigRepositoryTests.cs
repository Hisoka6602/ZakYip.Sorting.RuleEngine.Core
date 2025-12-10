using LiteDB;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Repositories;

/// <summary>
/// LiteDB DWS配置仓储测试
/// LiteDB DWS config repository tests
/// </summary>
public class LiteDbDwsConfigRepositoryTests : IDisposable
{
    private readonly ILiteDatabase _database;
    private readonly LiteDbDwsConfigRepository _repository;
    private readonly string _tempDbPath;

    public LiteDbDwsConfigRepositoryTests()
    {
        // 创建临时数据库文件
        // Create temporary database file
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_dws_config_{Guid.NewGuid()}.db");
        _database = new LiteDatabase(_tempDbPath);
        
        // 配置ID映射：将ConfigId映射为LiteDB的_id字段
        // Configure ID mapping: Map ConfigId to LiteDB's _id field
        _database.Mapper.Entity<DwsConfig>()
            .Id(x => x.ConfigId);
        
        _repository = new LiteDbDwsConfigRepository(_database);
    }

    [Fact]
    public async Task AddAsync_ShouldAddConfig_Successfully()
    {
        // Arrange
        var config = new DwsConfig
        {
            ConfigId = "dws-test-001",
            Name = "Test DWS Config",
            Mode = "Server",
            Host = "0.0.0.0",
            Port = 8081,
            DataTemplateId = "template-001",
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        var result = await _repository.AddAsync(config);

        // Assert
        Assert.True(result);
        
        // 验证可以通过ConfigId查询到配置
        // Verify config can be queried by ConfigId
        var retrieved = await _repository.GetByIdAsync("dws-test-001");
        Assert.NotNull(retrieved);
        Assert.Equal("dws-test-001", retrieved.ConfigId);
        Assert.Equal("Test DWS Config", retrieved.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnConfig_WhenExists()
    {
        // Arrange
        var config = new DwsConfig
        {
            ConfigId = "dws-test-002",
            Name = "Test Config 2",
            Mode = "Client",
            Host = "192.168.1.100",
            Port = 8082,
            DataTemplateId = "template-002",
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        await _repository.AddAsync(config);

        // Act
        var result = await _repository.GetByIdAsync("dws-test-002");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("dws-test-002", result.ConfigId);
        Assert.Equal("Test Config 2", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateConfig_Successfully()
    {
        // Arrange
        var config = new DwsConfig
        {
            ConfigId = "dws-test-003",
            Name = "Original Name",
            Mode = "Server",
            Host = "0.0.0.0",
            Port = 8083,
            DataTemplateId = "template-003",
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        await _repository.AddAsync(config);

        // 更新配置
        // Update config
        var updatedConfig = config with 
        { 
            Name = "Updated Name",
            Port = 9999
        };

        // Act
        var result = await _repository.UpdateAsync(updatedConfig);

        // Assert
        Assert.True(result);
        
        // 验证更新成功
        // Verify update was successful
        var retrieved = await _repository.GetByIdAsync("dws-test-003");
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Name", retrieved.Name);
        Assert.Equal(9999, retrieved.Port);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteConfig_Successfully()
    {
        // Arrange
        var config = new DwsConfig
        {
            ConfigId = "dws-test-004",
            Name = "Config to Delete",
            Mode = "Server",
            Host = "0.0.0.0",
            Port = 8084,
            DataTemplateId = "template-004",
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        await _repository.AddAsync(config);
        
        // 验证配置存在
        // Verify config exists
        var existsBefore = await _repository.GetByIdAsync("dws-test-004");
        Assert.NotNull(existsBefore);

        // Act
        var result = await _repository.DeleteAsync("dws-test-004");

        // Assert
        Assert.True(result);
        
        // 验证配置已删除
        // Verify config was deleted
        var existsAfter = await _repository.GetByIdAsync("dws-test-004");
        Assert.Null(existsAfter);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenConfigNotExists()
    {
        // Act
        var result = await _repository.DeleteAsync("non-existent-id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetEnabledConfigsAsync_ShouldReturnOnlyEnabledConfigs()
    {
        // Arrange
        var enabledConfig = new DwsConfig
        {
            ConfigId = "dws-enabled",
            Name = "Enabled Config",
            Mode = "Server",
            Host = "0.0.0.0",
            Port = 8085,
            DataTemplateId = "template-005",
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        
        var disabledConfig = new DwsConfig
        {
            ConfigId = "dws-disabled",
            Name = "Disabled Config",
            Mode = "Server",
            Host = "0.0.0.0",
            Port = 8086,
            DataTemplateId = "template-006",
            IsEnabled = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        
        await _repository.AddAsync(enabledConfig);
        await _repository.AddAsync(disabledConfig);

        // Act
        var result = await _repository.GetEnabledConfigsAsync();

        // Assert
        var configs = result.ToList();
        Assert.Single(configs);
        Assert.Equal("dws-enabled", configs[0].ConfigId);
        Assert.True(configs[0].IsEnabled);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllConfigs()
    {
        // Arrange
        var config1 = new DwsConfig
        {
            ConfigId = "dws-all-001",
            Name = "Config 1",
            Mode = "Server",
            Host = "0.0.0.0",
            Port = 8087,
            DataTemplateId = "template-007",
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        
        var config2 = new DwsConfig
        {
            ConfigId = "dws-all-002",
            Name = "Config 2",
            Mode = "Client",
            Host = "192.168.1.1",
            Port = 8088,
            DataTemplateId = "template-008",
            IsEnabled = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        
        await _repository.AddAsync(config1);
        await _repository.AddAsync(config2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var configs = result.ToList();
        Assert.True(configs.Count >= 2);
        Assert.Contains(configs, c => c.ConfigId == "dws-all-001");
        Assert.Contains(configs, c => c.ConfigId == "dws-all-002");
    }

    public void Dispose()
    {
        // 清理资源
        // Clean up resources
        _database?.Dispose();
        
        // 删除临时数据库文件
        // Delete temporary database file
        if (File.Exists(_tempDbPath))
        {
            try
            {
                File.Delete(_tempDbPath);
            }
            catch
            {
                // 忽略删除失败
                // Ignore deletion failures
            }
        }
        
        GC.SuppressFinalize(this);
    }
}
