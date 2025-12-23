using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// WCS API适配器工厂实现（支持热更新）
/// WCS API adapter factory implementation (with hot reload support)
/// 根据配置和自动应答模式选择激活的API适配器
/// Selects the active API adapter based on configuration and auto-response mode
/// </summary>
public class WcsApiAdapterFactory : IWcsApiAdapterFactory
{
    private readonly IEnumerable<IWcsApiAdapter> _allAdapters;
    private readonly IWcsApiAdapter _mockAdapter;
    private readonly IAutoResponseModeService _autoResponseModeService;
    private readonly IWcsApiConfigRepository _configRepository;
    private readonly ILogger<WcsApiAdapterFactory> _logger;
    private readonly string _fallbackAdapterName;
    private readonly object _cacheLock = new();
    
    private IWcsApiAdapter? _cachedConfiguredAdapter;
    private string? _cachedAdapterName;

    public WcsApiAdapterFactory(
        IEnumerable<IWcsApiAdapter> adapters,
        string fallbackAdapterType,
        IAutoResponseModeService autoResponseModeService,
        IWcsApiConfigRepository configRepository,
        ILogger<WcsApiAdapterFactory> logger)
    {
        _logger = logger;
        _autoResponseModeService = autoResponseModeService;
        _configRepository = configRepository;
        _allAdapters = adapters;
        _fallbackAdapterName = fallbackAdapterType;
        
        // 查找模拟适配器
        // Find mock adapter
        _mockAdapter = adapters.FirstOrDefault(a => a is MockWcsApiAdapter)
            ?? throw new InvalidOperationException("未找到模拟WCS API适配器 / Mock WCS API adapter not found");
        
        _logger.LogInformation(
            "WCS API适配器工厂已初始化，后备适配器: {AdapterName}",
            _fallbackAdapterName);
    }

    /// <summary>
    /// 获取当前激活的API适配器（支持热更新）
    /// Get the currently active API adapter (with hot reload support)
    /// 如果自动应答模式启用，返回模拟适配器；否则从LiteDB读取配置并返回对应的适配器
    /// Returns mock adapter if auto-response mode is enabled, otherwise reads config from LiteDB and returns the configured adapter
    /// </summary>
    public IWcsApiAdapter GetActiveAdapter()
    {
        if (_autoResponseModeService.IsEnabled)
        {
            _logger.LogDebug("自动应答模式已启用，使用模拟适配器");
            return _mockAdapter;
        }

        // 从LiteDB读取配置（支持热更新）
        // Read config from LiteDB (supports hot reload)
        // 注意：这里使用同步阻塞调用是因为接口定义为同步方法，且工厂在Singleton生命周期中
        // Note: Using sync-over-async here because interface is synchronous and factory is in Singleton lifetime
        var config = _configRepository.GetByIdAsync(WcsApiConfig.SingletonId).GetAwaiter().GetResult();
        
        string adapterTypeName;
        if (config != null && !string.IsNullOrEmpty(config.ActiveAdapterType))
        {
            adapterTypeName = config.ActiveAdapterType;
            _logger.LogDebug("从LiteDB读取适配器配置: {AdapterName}", adapterTypeName);
        }
        else
        {
            adapterTypeName = _fallbackAdapterName;
            _logger.LogDebug("使用后备适配器: {AdapterName}", adapterTypeName);
        }

        // 使用锁保护缓存的读写操作，确保线程安全
        // Use lock to protect cache read/write operations for thread safety
        lock (_cacheLock)
        {
            // 如果缓存的适配器名称与当前配置一致，直接返回缓存
            // If cached adapter name matches current config, return cached adapter
            if (_cachedAdapterName == adapterTypeName && _cachedConfiguredAdapter != null)
            {
                return _cachedConfiguredAdapter;
            }

            // 查找对应的适配器
            // Find the corresponding adapter
            var adapter = _allAdapters
                .Where(a => a is not MockWcsApiAdapter)
                .FirstOrDefault(a => a.GetType().Name == adapterTypeName)
                ?? _allAdapters.FirstOrDefault(a => a is not MockWcsApiAdapter)
                ?? throw new InvalidOperationException("未找到可用的WCS API适配器 / No WCS API adapter found");

            // 更新缓存
            // Update cache
            _cachedConfiguredAdapter = adapter;
            _cachedAdapterName = adapterTypeName;
            
            _logger.LogInformation("切换到API适配器: {AdapterName}", adapterTypeName);
            return adapter;
        }
    }

    /// <summary>
    /// 获取适配器类型名称（支持热更新）
    /// Get the adapter type name (with hot reload support)
    /// </summary>
    public string GetActiveAdapterName()
    {
        if (_autoResponseModeService.IsEnabled)
        {
            return "MockWcsApiAdapter";
        }

        // 从LiteDB读取配置
        // 注意：这里使用同步阻塞调用是因为接口定义为同步方法
        // Note: Using sync-over-async here because interface is synchronous
        var config = _configRepository.GetByIdAsync(WcsApiConfig.SingletonId).GetAwaiter().GetResult();
        
        if (config != null && !string.IsNullOrEmpty(config.ActiveAdapterType))
        {
            return config.ActiveAdapterType;
        }

        return _fallbackAdapterName;
    }

    /// <summary>
    /// 清空缓存，强制下次调用时重新加载配置（用于热更新）
    /// Clear cache to force reload on next call (for hot reload)
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedConfiguredAdapter = null;
            _cachedAdapterName = null;
            _logger.LogInformation("WCS API适配器缓存已清空，下次调用将重新加载配置 / WCS API adapter cache cleared, will reload config on next call");
        }
    }
}
