using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.BackgroundServices;

/// <summary>
/// 配置缓存预加载服务
/// 在程序启动时预先加载配置数据到缓存
/// </summary>
public class ConfigurationCachePreloadService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConfigurationCachePreloadService> _logger;

    public ConfigurationCachePreloadService(
        IServiceProvider serviceProvider,
        ILogger<ConfigurationCachePreloadService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始预加载配置缓存...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<Services.ConfigurationCacheService>();
            var chuteRepository = scope.ServiceProvider.GetRequiredService<IChuteRepository>();
            var ruleRepository = scope.ServiceProvider.GetRequiredService<IRuleRepository>();
            var apiConfigRepository = scope.ServiceProvider.GetRequiredService<IThirdPartyApiConfigRepository>();

            // 预加载格口缓存
            await cacheService.ReloadChuteCacheAsync(chuteRepository, cancellationToken);

            // 预加载分拣规则缓存
            await cacheService.ReloadSortingRuleCacheAsync(ruleRepository, cancellationToken);

            // 预加载第三方API配置缓存
            await cacheService.ReloadThirdPartyApiConfigCacheAsync(apiConfigRepository);

            _logger.LogInformation("配置缓存预加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置缓存预加载失败");
            // 不抛出异常，允许应用继续启动
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("配置缓存预加载服务停止");
        return Task.CompletedTask;
    }
}
