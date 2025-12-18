using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 邮政分揽投机构配置的LiteDB仓储实现
/// LiteDB repository implementation for Postal Collection configuration
/// </summary>
public class LiteDbPostCollectionConfigRepository : BaseLiteDbRepository<PostCollectionConfig, string>, IPostCollectionConfigRepository
{
    private const string CollectionName = "post_collection_configs";

    public LiteDbPostCollectionConfigRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void ConfigureIdMapping()
    {
        Database.Mapper.Entity<PostCollectionConfig>()
            .Id(x => x.ConfigId);
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        // ConfigId is now the primary key (_id), no need for a separate unique index
        collection.EnsureIndex(x => x.IsEnabled);
    }

    protected override string GetEntityId(PostCollectionConfig entity) => entity.ConfigId;

    protected override PostCollectionConfig UpdateTimestamp(PostCollectionConfig entity) =>
        entity with { UpdatedAt = Clock.LocalNow };

    public Task<IEnumerable<PostCollectionConfig>> GetEnabledConfigsAsync()
    {
        var collection = GetCollection();
        var configs = collection
            .Find(x => x.IsEnabled)
            .ToList();
        return Task.FromResult<IEnumerable<PostCollectionConfig>>(configs);
    }
}
