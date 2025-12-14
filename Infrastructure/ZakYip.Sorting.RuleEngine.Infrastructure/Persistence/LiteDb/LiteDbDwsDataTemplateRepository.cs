using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// DWS数据模板的LiteDB仓储实现
/// LiteDB repository implementation for DWS data template
/// </summary>
public class LiteDbDwsDataTemplateRepository : BaseLiteDbRepository<DwsDataTemplate, long>, IDwsDataTemplateRepository
{
    private const string CollectionName = "dws_data_templates";

    public LiteDbDwsDataTemplateRepository(ILiteDatabase database) 
        : base(database, CollectionName)
    {
    }

    protected override void EnsureIndexes()
    {
        var collection = GetCollection();
        collection.EnsureIndex(x => x.TemplateId, unique: true);
        collection.EnsureIndex(x => x.IsEnabled);
    }

    protected override long GetEntityId(DwsDataTemplate entity) => entity.TemplateId;

    protected override DwsDataTemplate UpdateTimestamp(DwsDataTemplate entity) =>
        entity with { UpdatedAt = DateTime.Now };

    public Task<IEnumerable<DwsDataTemplate>> GetEnabledTemplatesAsync()
    {
        var collection = GetCollection();
        var templates = collection
            .Find(x => x.IsEnabled)
            .ToList();
        return Task.FromResult<IEnumerable<DwsDataTemplate>>(templates);
    }
}
