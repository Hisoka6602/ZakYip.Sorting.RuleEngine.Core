using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 第三方API配置的LiteDB仓储实现
/// </summary>
public class LiteDbThirdPartyApiConfigRepository : IThirdPartyApiConfigRepository
{
    private readonly ILiteDatabase _database;
    private const string CollectionName = "third_party_api_configs";

    public LiteDbThirdPartyApiConfigRepository(ILiteDatabase database)
    {
        _database = database;
        
        // 创建索引以提高查询性能
        var collection = _database.GetCollection<ThirdPartyApiConfig>(CollectionName);
        collection.EnsureIndex(x => x.ConfigId, unique: true);
        collection.EnsureIndex(x => x.IsEnabled);
        collection.EnsureIndex(x => x.Priority);
    }

    public Task<IEnumerable<ThirdPartyApiConfig>> GetEnabledConfigsAsync()
    {
        var collection = _database.GetCollection<ThirdPartyApiConfig>(CollectionName);
        var configs = collection
            .Find(x => x.IsEnabled)
            .OrderBy(x => x.Priority)
            .ToList();
        return Task.FromResult<IEnumerable<ThirdPartyApiConfig>>(configs);
    }

    public Task<ThirdPartyApiConfig?> GetByIdAsync(string configId)
    {
        var collection = _database.GetCollection<ThirdPartyApiConfig>(CollectionName);
        var config = collection.FindById(configId);
        return Task.FromResult<ThirdPartyApiConfig?>(config);
    }

    public Task<IEnumerable<ThirdPartyApiConfig>> GetAllAsync()
    {
        var collection = _database.GetCollection<ThirdPartyApiConfig>(CollectionName);
        var configs = collection.FindAll().ToList();
        return Task.FromResult<IEnumerable<ThirdPartyApiConfig>>(configs);
    }

    public Task<bool> AddAsync(ThirdPartyApiConfig config)
    {
        var collection = _database.GetCollection<ThirdPartyApiConfig>(CollectionName);
        var result = collection.Insert(config);
        return Task.FromResult(result != null);
    }

    public Task<bool> UpdateAsync(ThirdPartyApiConfig config)
    {
        var collection = _database.GetCollection<ThirdPartyApiConfig>(CollectionName);
        var updatedConfig = config with { UpdatedAt = DateTime.Now };
        var result = collection.Update(updatedConfig);
        return Task.FromResult(result);
    }

    public Task<bool> DeleteAsync(string configId)
    {
        var collection = _database.GetCollection<ThirdPartyApiConfig>(CollectionName);
        var result = collection.Delete(configId);
        return Task.FromResult(result);
    }
}
