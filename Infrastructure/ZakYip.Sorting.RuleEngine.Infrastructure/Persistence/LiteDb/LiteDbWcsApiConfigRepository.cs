using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// WCS API配置的LiteDB仓储实现
/// </summary>
public class LiteDbWcsApiConfigRepository : IWcsApiConfigRepository
{
    private readonly ILiteDatabase _database;
    private const string CollectionName = "third_party_api_configs";

    public LiteDbWcsApiConfigRepository(ILiteDatabase database)
    {
        _database = database;
        
        // 创建索引以提高查询性能
        var collection = _database.GetCollection<WcsApiConfig>(CollectionName);
        collection.EnsureIndex(x => x.ConfigId, unique: true);
        collection.EnsureIndex(x => x.IsEnabled);
        collection.EnsureIndex(x => x.Priority);
    }

    public Task<IEnumerable<WcsApiConfig>> GetEnabledConfigsAsync()
    {
        var collection = _database.GetCollection<WcsApiConfig>(CollectionName);
        var configs = collection
            .Find(x => x.IsEnabled)
            .OrderBy(x => x.Priority)
            .ToList();
        return Task.FromResult<IEnumerable<WcsApiConfig>>(configs);
    }

    public Task<WcsApiConfig?> GetByIdAsync(long configId)
    {
        var collection = _database.GetCollection<WcsApiConfig>(CollectionName);
        var config = collection.FindById(new BsonValue(configId));
        return Task.FromResult<WcsApiConfig?>(config);
    }

    public Task<IEnumerable<WcsApiConfig>> GetAllAsync()
    {
        var collection = _database.GetCollection<WcsApiConfig>(CollectionName);
        var configs = collection.FindAll().ToList();
        return Task.FromResult<IEnumerable<WcsApiConfig>>(configs);
    }

    public Task<bool> AddAsync(WcsApiConfig config)
    {
        var collection = _database.GetCollection<WcsApiConfig>(CollectionName);
        var result = collection.Insert(config);
        return Task.FromResult(result != null);
    }

    public Task<bool> UpdateAsync(WcsApiConfig config)
    {
        var collection = _database.GetCollection<WcsApiConfig>(CollectionName);
        var updatedConfig = config with { UpdatedAt = DateTime.Now };
        var result = collection.Update(updatedConfig);
        return Task.FromResult(result);
    }

    public Task<bool> DeleteAsync(long configId)
    {
        var collection = _database.GetCollection<WcsApiConfig>(CollectionName);
        var result = collection.Delete(new BsonValue(configId));
        return Task.FromResult(result);
    }
}
