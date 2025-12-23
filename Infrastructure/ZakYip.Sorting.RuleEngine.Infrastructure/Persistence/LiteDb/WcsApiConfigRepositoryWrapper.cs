using Microsoft.Extensions.DependencyInjection;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// WCS API配置仓储包装器（用于从Singleton访问Scoped服务）
/// WCS API config repository wrapper (for accessing Scoped service from Singleton)
/// </summary>
public sealed class WcsApiConfigRepositoryWrapper : IWcsApiConfigRepository
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public WcsApiConfigRepositoryWrapper(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task<IEnumerable<WcsApiConfig>> GetAllAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWcsApiConfigRepository>();
        return await repository.GetAllAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<WcsApiConfig>> GetEnabledConfigsAsync()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWcsApiConfigRepository>();
        return await repository.GetEnabledConfigsAsync().ConfigureAwait(false);
    }

    public async Task<WcsApiConfig?> GetByIdAsync(string configId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWcsApiConfigRepository>();
        return await repository.GetByIdAsync(configId).ConfigureAwait(false);
    }

    public async Task<bool> AddAsync(WcsApiConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWcsApiConfigRepository>();
        return await repository.AddAsync(config).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(WcsApiConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWcsApiConfigRepository>();
        return await repository.UpdateAsync(config).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string configId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWcsApiConfigRepository>();
        return await repository.DeleteAsync(configId).ConfigureAwait(false);
    }

    public async Task<bool> UpsertAsync(WcsApiConfig config)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWcsApiConfigRepository>();
        return await repository.UpsertAsync(config).ConfigureAwait(false);
    }
}
