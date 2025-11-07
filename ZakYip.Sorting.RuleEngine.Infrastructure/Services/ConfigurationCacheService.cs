using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Services;

/// <summary>
/// 配置缓存服务
/// 用于缓存常用配置数据（如格口、规则、WCS API配置）
/// </summary>
public class ConfigurationCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigurationCacheService> _logger;
    
    private const string ChutesCacheKey = "AllChutes";
    private const string EnabledChutesCacheKey = "EnabledChutes";
    private const string SortingRulesCacheKey = "AllSortingRules";
    private const string EnabledSortingRulesCacheKey = "EnabledSortingRules";
    private const string ThirdPartyApiConfigsCacheKey = "AllThirdPartyApiConfigs";
    private const string EnabledThirdPartyApiConfigsCacheKey = "EnabledThirdPartyApiConfigs";
    
    // 缓存过期时间（默认1小时）
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(1);

    public ConfigurationCacheService(
        IMemoryCache cache,
        ILogger<ConfigurationCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    #region Chute缓存

    /// <summary>
    /// 获取所有格口（从缓存）
    /// </summary>
    public async Task<IEnumerable<Chute>> GetAllChutesAsync(
        IChuteRepository repository, 
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(ChutesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            entry.Size = 1;
            var chutes = await repository.GetAllAsync(cancellationToken);
            _logger.LogInformation("格口数据已缓存，共 {Count} 条", chutes.Count());
            return chutes;
        }) ?? Enumerable.Empty<Chute>();
    }

    /// <summary>
    /// 获取启用的格口（从缓存）
    /// </summary>
    public async Task<IEnumerable<Chute>> GetEnabledChutesAsync(
        IChuteRepository repository, 
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(EnabledChutesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            entry.Size = 1;
            var chutes = await repository.GetEnabledChutesAsync(cancellationToken);
            _logger.LogInformation("启用格口数据已缓存，共 {Count} 条", chutes.Count());
            return chutes;
        }) ?? Enumerable.Empty<Chute>();
    }

    /// <summary>
    /// 重新加载格口缓存
    /// </summary>
    public async Task ReloadChuteCacheAsync(
        IChuteRepository repository, 
        CancellationToken cancellationToken = default)
    {
        _cache.Remove(ChutesCacheKey);
        _cache.Remove(EnabledChutesCacheKey);
        
        await GetAllChutesAsync(repository, cancellationToken);
        await GetEnabledChutesAsync(repository, cancellationToken);
        
        _logger.LogInformation("格口缓存已重新加载");
    }

    #endregion

    #region SortingRule缓存

    /// <summary>
    /// 获取所有分拣规则（从缓存）
    /// </summary>
    public async Task<IEnumerable<SortingRule>> GetAllSortingRulesAsync(
        IRuleRepository repository, 
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(SortingRulesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            entry.Size = 1;
            var rules = await repository.GetAllAsync(cancellationToken);
            _logger.LogInformation("分拣规则数据已缓存，共 {Count} 条", rules.Count());
            return rules;
        }) ?? Enumerable.Empty<SortingRule>();
    }

    /// <summary>
    /// 获取启用的分拣规则（从缓存）
    /// </summary>
    public async Task<IEnumerable<SortingRule>> GetEnabledSortingRulesAsync(
        IRuleRepository repository, 
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(EnabledSortingRulesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            entry.Size = 1;
            var rules = await repository.GetEnabledRulesAsync(cancellationToken);
            _logger.LogInformation("启用分拣规则数据已缓存，共 {Count} 条", rules.Count());
            return rules;
        }) ?? Enumerable.Empty<SortingRule>();
    }

    /// <summary>
    /// 重新加载分拣规则缓存
    /// </summary>
    public async Task ReloadSortingRuleCacheAsync(
        IRuleRepository repository, 
        CancellationToken cancellationToken = default)
    {
        _cache.Remove(SortingRulesCacheKey);
        _cache.Remove(EnabledSortingRulesCacheKey);
        
        await GetAllSortingRulesAsync(repository, cancellationToken);
        await GetEnabledSortingRulesAsync(repository, cancellationToken);
        
        _logger.LogInformation("分拣规则缓存已重新加载");
    }

    #endregion

    #region ThirdPartyApiConfig缓存

    /// <summary>
    /// 获取所有WCS API配置（从缓存）
    /// </summary>
    public async Task<IEnumerable<WcsApiConfig>> GetAllThirdPartyApiConfigsAsync(
        IWcsApiConfigRepository repository)
    {
        return await _cache.GetOrCreateAsync(ThirdPartyApiConfigsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            entry.Size = 1;
            var configs = await repository.GetAllAsync();
            _logger.LogInformation("WCS API配置数据已缓存，共 {Count} 条", configs.Count());
            return configs;
        }) ?? Enumerable.Empty<WcsApiConfig>();
    }

    /// <summary>
    /// 获取启用的WCS API配置（从缓存）
    /// </summary>
    public async Task<IEnumerable<WcsApiConfig>> GetEnabledThirdPartyApiConfigsAsync(
        IWcsApiConfigRepository repository)
    {
        return await _cache.GetOrCreateAsync(EnabledThirdPartyApiConfigsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheExpiration;
            entry.Size = 1;
            var configs = await repository.GetEnabledConfigsAsync();
            _logger.LogInformation("启用WCS API配置数据已缓存，共 {Count} 条", configs.Count());
            return configs;
        }) ?? Enumerable.Empty<WcsApiConfig>();
    }

    /// <summary>
    /// 重新加载WCS API配置缓存
    /// </summary>
    public async Task ReloadThirdPartyApiConfigCacheAsync(
        IWcsApiConfigRepository repository)
    {
        _cache.Remove(ThirdPartyApiConfigsCacheKey);
        _cache.Remove(EnabledThirdPartyApiConfigsCacheKey);
        
        await GetAllThirdPartyApiConfigsAsync(repository);
        await GetEnabledThirdPartyApiConfigsAsync(repository);
        
        _logger.LogInformation("WCS API配置缓存已重新加载");
    }

    #endregion

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearAllCache()
    {
        _cache.Remove(ChutesCacheKey);
        _cache.Remove(EnabledChutesCacheKey);
        _cache.Remove(SortingRulesCacheKey);
        _cache.Remove(EnabledSortingRulesCacheKey);
        _cache.Remove(ThirdPartyApiConfigsCacheKey);
        _cache.Remove(EnabledThirdPartyApiConfigsCacheKey);
        
        _logger.LogInformation("所有配置缓存已清除");
    }
}
