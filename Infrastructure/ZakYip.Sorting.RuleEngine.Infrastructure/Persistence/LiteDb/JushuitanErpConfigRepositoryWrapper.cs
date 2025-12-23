using Microsoft.Extensions.DependencyInjection;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 聚水潭ERP配置仓储包装器（用于从Singleton访问Scoped服务）
/// Jushuituan ERP config repository wrapper (for accessing Scoped service from Singleton)
/// </summary>
public sealed class JushuitanErpConfigRepositoryWrapper : IJushuitanErpConfigRepository
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public JushuitanErpConfigRepositoryWrapper(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IEnumerable<JushuitanErpConfig>> GetAllAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbJushuitanErpConfigRepository>();
        return await repository.GetAllAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<JushuitanErpConfig>> GetEnabledConfigsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbJushuitanErpConfigRepository>();
        return await repository.GetEnabledConfigsAsync().ConfigureAwait(false);
    }

    public async Task<JushuitanErpConfig?> GetByIdAsync(string configId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbJushuitanErpConfigRepository>();
        return await repository.GetByIdAsync(configId).ConfigureAwait(false);
    }

    public async Task<bool> AddAsync(JushuitanErpConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbJushuitanErpConfigRepository>();
        return await repository.AddAsync(config).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(JushuitanErpConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbJushuitanErpConfigRepository>();
        return await repository.UpdateAsync(config).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string configId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbJushuitanErpConfigRepository>();
        return await repository.DeleteAsync(configId).ConfigureAwait(false);
    }

    public async Task<bool> UpsertAsync(JushuitanErpConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbJushuitanErpConfigRepository>();
        return await repository.UpsertAsync(config).ConfigureAwait(false);
    }
}
