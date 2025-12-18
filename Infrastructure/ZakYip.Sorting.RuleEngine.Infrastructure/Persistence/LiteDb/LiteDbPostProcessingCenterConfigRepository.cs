using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 邮政处理中心配置的LiteDB仓储实现
/// LiteDB repository implementation for Postal Processing Center configuration
/// </summary>
public class LiteDbPostProcessingCenterConfigRepository : BaseLiteDbRepository<PostProcessingCenterConfig, string>, IPostProcessingCenterConfigRepository
{
    private const string CollectionName = "post_processing_center_configs";

    public LiteDbPostProcessingCenterConfigRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void ConfigureIdMapping()
    {
        Database.Mapper.Entity<PostProcessingCenterConfig>()
            .Id(x => x.ConfigId);
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        // ConfigId is now the primary key (_id), no need for a separate unique index
        collection.EnsureIndex(x => x.IsEnabled);
    }

    protected override string GetEntityId(PostProcessingCenterConfig entity) => entity.ConfigId;

    protected override PostProcessingCenterConfig UpdateTimestamp(PostProcessingCenterConfig entity) =>
        entity with { UpdatedAt = Clock.LocalNow };

    public Task<IEnumerable<PostProcessingCenterConfig>> GetEnabledConfigsAsync()
    {
        var collection = GetCollection();
        var configs = collection
            .Find(x => x.IsEnabled)
            .ToList();
        return Task.FromResult<IEnumerable<PostProcessingCenterConfig>>(configs);
    }
}
