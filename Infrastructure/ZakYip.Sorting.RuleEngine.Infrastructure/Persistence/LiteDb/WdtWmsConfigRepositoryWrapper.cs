using Microsoft.Extensions.DependencyInjection;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// 旺店通WMS配置仓储包装器（用于从Singleton访问Scoped服务）
/// WDT WMS config repository wrapper (for accessing Scoped service from Singleton)
/// </summary>
public sealed class WdtWmsConfigRepositoryWrapper : IWdtWmsConfigRepository
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public WdtWmsConfigRepositoryWrapper(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IEnumerable<WdtWmsConfig>> GetAllAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbWdtWmsConfigRepository>();
        return await repository.GetAllAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<WdtWmsConfig>> GetEnabledConfigsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbWdtWmsConfigRepository>();
        return await repository.GetEnabledConfigsAsync().ConfigureAwait(false);
    }

    public async Task<WdtWmsConfig?> GetByIdAsync(string configId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbWdtWmsConfigRepository>();
        return await repository.GetByIdAsync(configId).ConfigureAwait(false);
    }

    public async Task<bool> AddAsync(WdtWmsConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbWdtWmsConfigRepository>();
        return await repository.AddAsync(config).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(WdtWmsConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbWdtWmsConfigRepository>();
        return await repository.UpdateAsync(config).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string configId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbWdtWmsConfigRepository>();
        return await repository.DeleteAsync(configId).ConfigureAwait(false);
    }

    public async Task<bool> UpsertAsync(WdtWmsConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<LiteDbWdtWmsConfigRepository>();
        return await repository.UpsertAsync(config).ConfigureAwait(false);
    }
}
