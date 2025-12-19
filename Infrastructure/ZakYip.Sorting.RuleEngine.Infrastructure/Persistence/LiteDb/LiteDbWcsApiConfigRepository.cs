using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// WCS API配置的LiteDB仓储实现
/// LiteDB repository implementation for WCS API configuration
/// </summary>
public class LiteDbWcsApiConfigRepository : BaseLiteDbRepository<WcsApiConfig, string>, IWcsApiConfigRepository
{
    private const string CollectionName = "wcsapi_configs";

    public LiteDbWcsApiConfigRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void ConfigureIdMapping()
    {
        Database.Mapper.Entity<WcsApiConfig>()
            .Id(x => x.ConfigId);
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        // ConfigId is now the primary key (_id), no need for a separate unique index
        collection.EnsureIndex(x => x.IsEnabled);
    }

    protected override string GetEntityId(WcsApiConfig entity) => entity.ConfigId;

    protected override WcsApiConfig UpdateTimestamp(WcsApiConfig entity) =>
        entity with { UpdatedAt = Clock.LocalNow };
    
    public Task<IEnumerable<WcsApiConfig>> GetEnabledConfigsAsync()
    {
        var collection = GetCollection();
        var configs = collection
            .Find(x => x.IsEnabled)
            .ToList();
        return Task.FromResult<IEnumerable<WcsApiConfig>>(configs);
    }
}
