using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 聚水潭ERP配置的LiteDB仓储实现
/// LiteDB repository implementation for Jushuituan ERP configuration
/// </summary>
public class LiteDbJushuitanErpConfigRepository : BaseLiteDbRepository<JushuitanErpConfig, string>, IJushuitanErpConfigRepository
{
    private const string CollectionName = "jushuitanerp_configs";

    public LiteDbJushuitanErpConfigRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        collection.EnsureIndex(x => x.ConfigId, unique: true);
        collection.EnsureIndex(x => x.IsEnabled);
    }

    protected override string GetEntityId(JushuitanErpConfig entity) => entity.ConfigId;

    protected override JushuitanErpConfig UpdateTimestamp(JushuitanErpConfig entity) =>
        entity with { UpdatedAt = Clock.LocalNow };

    public Task<IEnumerable<JushuitanErpConfig>> GetEnabledConfigsAsync()
    {
        var collection = GetCollection();
        var configs = collection
            .Find(x => x.IsEnabled)
            .ToList();
        return Task.FromResult<IEnumerable<JushuitanErpConfig>>(configs);
    }
}
