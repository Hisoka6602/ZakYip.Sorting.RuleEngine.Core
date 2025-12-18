using LiteDB;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.Mappers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using SystemJsonSerializer = System.Text.Json.JsonSerializer;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Repositories;

/// <summary>
/// 测试确保LiteDB内部字段不会暴露在API响应中
/// Tests to ensure LiteDB internal fields are not exposed in API responses
/// </summary>
public class LiteDbIdExposureTests : IDisposable
{
    private readonly ILiteDatabase _database;
    private readonly string _tempDbPath;

    public LiteDbIdExposureTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_id_exposure_{Guid.NewGuid()}.db");
        _database = new LiteDatabase(_tempDbPath);
        
        // 配置ID映射
        // Configure ID mapping
        _database.Mapper.Entity<DwsConfig>()
            .Id(x => x.ConfigId);
    }

    [Fact]
    public void DwsConfigResponseDto_ShouldNotContain_InternalIdField()
    {
        // Arrange
        var collection = _database.GetCollection<DwsConfig>("dws_configs");
        var config = new DwsConfig
        {
            ConfigId = "TestDwsConfig1",
            Mode = "Server",
            Host = "0.0.0.0",
            Port = 8081,
            DataTemplateId = 1L,
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        
        collection.Insert(config);
        var retrievedConfig = collection.FindById(new BsonValue(1001L));

        // Act
        var dto = retrievedConfig.ToResponseDto();
        var json = SystemJsonSerializer.Serialize(dto);

        // Assert
        Assert.NotNull(dto);
        
        // 验证JSON中不包含 _id 或 ConfigId 字段（单例模式不暴露ID）
        // Verify JSON does not contain _id or ConfigId field (singleton pattern does not expose ID)
        Assert.DoesNotContain("\"_id\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"ConfigId\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"Id\":", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DwsConfigResponseDto_OnlyContains_ExpectedFields()
    {
        // Arrange
        var config = new DwsConfig
        {
            ConfigId = "TestDwsConfig2",
            Mode = "Client",
            Host = "192.168.1.1",
            Port = 8082,
            DataTemplateId = 2L,
            IsEnabled = true,
            MaxConnections = 100,
            ReceiveBufferSize = 4096,
            SendBufferSize = 4096,
            TimeoutSeconds = 30,
            AutoReconnect = true,
            ReconnectIntervalSeconds = 5,
            Description = "Test description",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        var dto = config.ToResponseDto();
        var json = SystemJsonSerializer.Serialize(dto, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        // Assert - 验证DTO只包含预期的字段（单例模式不包含ConfigId）
        // Verify DTO only contains expected fields (singleton pattern does not include ConfigId)
        var expectedFields = new[]
        {
            "Name",
            "Mode",
            "Host",
            "Port",
            "DataTemplateId",
            "IsEnabled",
            "MaxConnections",
            "ReceiveBufferSize",
            "SendBufferSize",
            "TimeoutSeconds",
            "AutoReconnect",
            "ReconnectIntervalSeconds",
            "Description",
            "CreatedAt",
            "UpdatedAt"
        };

        foreach (var field in expectedFields)
        {
            Assert.Contains($"\"{field}\"", json, StringComparison.OrdinalIgnoreCase);
        }

        // 验证不包含LiteDB内部字段和ConfigId（单例模式）
        // Verify no LiteDB internal fields and ConfigId (singleton pattern)
        Assert.DoesNotContain("\"_id\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"_type\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"ConfigId\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DwsDataTemplateResponseDto_ShouldNotContain_InternalIdField()
    {
        // Arrange
        var template = new DwsDataTemplate
        {
            TemplateId = 1L,
            Template = "{Code},{Weight}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
            Description = "Test template",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Act
        var dto = template.ToResponseDto();
        var json = SystemJsonSerializer.Serialize(dto);

        // Assert - 单例模式不暴露TemplateId
        // Singleton pattern does not expose TemplateId
        Assert.DoesNotContain("\"_id\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"TemplateId\"", json, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _database?.Dispose();
        
        if (File.Exists(_tempDbPath))
        {
            try
            {
                File.Delete(_tempDbPath);
            }
            catch
            {
                // Ignore
            }
        }
        
        GC.SuppressFinalize(this);
    }
}
