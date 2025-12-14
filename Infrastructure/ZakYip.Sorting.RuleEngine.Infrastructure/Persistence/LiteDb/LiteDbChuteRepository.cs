using LiteDB;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB格口仓储实现
/// </summary>
public class LiteDbChuteRepository : IChuteRepository
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<Chute> _collection;

    public LiteDbChuteRepository(
        ILiteDatabase database,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_database = database;
        _collection = _database.GetCollection<Chute>("chutes");
        
        // 创建索引
        _collection.EnsureIndex(x => x.ChuteCode);
        _collection.EnsureIndex(x => x.IsEnabled);
        _clock = clock;
    }

    public Task<IEnumerable<Chute>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var chutes = _collection.FindAll();
        return Task.FromResult(chutes);
    }

    public Task<Chute?> GetByIdAsync(long chuteId, CancellationToken cancellationToken = default)
    {
        var chute = _collection.FindById(chuteId);
        return Task.FromResult<Chute?>(chute);
    }

    public Task<Chute?> GetByCodeAsync(string chuteCode, CancellationToken cancellationToken = default)
    {
        var chute = _collection.FindOne(x => x.ChuteCode == chuteCode);
        return Task.FromResult<Chute?>(chute);
    }

    public Task<Chute> AddAsync(Chute chute, CancellationToken cancellationToken = default)
    {
        chute.CreatedAt = _clock.LocalNow;
        chute.UpdatedAt = null;
        
        var id = _collection.Insert(chute);
        chute.ChuteId = id.AsInt64;
        
        return Task.FromResult(chute);
    }

    public Task UpdateAsync(Chute chute, CancellationToken cancellationToken = default)
    {
        chute.UpdatedAt = _clock.LocalNow;
        _collection.Update(chute);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(long chuteId, CancellationToken cancellationToken = default)
    {
        _collection.Delete(chuteId);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Chute>> GetEnabledChutesAsync(CancellationToken cancellationToken = default)
    {
        var chutes = _collection.Find(x => x.IsEnabled);
        return Task.FromResult(chutes);
    }
}
