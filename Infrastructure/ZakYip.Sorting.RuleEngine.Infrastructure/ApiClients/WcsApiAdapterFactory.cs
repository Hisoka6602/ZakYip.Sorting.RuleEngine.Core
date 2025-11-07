using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// WCS API适配器工厂实现
/// WCS API adapter factory implementation
/// 根据配置选择唯一激活的API适配器
/// Selects the single active API adapter based on configuration
/// </summary>
public class WcsApiAdapterFactory : IWcsApiAdapterFactory
{
    private readonly IWcsApiAdapter _activeAdapter;
    private readonly string _activeAdapterName;
    private readonly ILogger<WcsApiAdapterFactory> _logger;

    public WcsApiAdapterFactory(
        IEnumerable<IWcsApiAdapter> adapters,
        string activeAdapterType,
        ILogger<WcsApiAdapterFactory> logger)
    {
        _logger = logger;
        
        // 根据配置选择激活的适配器
        // Select the active adapter based on configuration
        _activeAdapter = adapters.FirstOrDefault(a => a.GetType().Name == activeAdapterType)
            ?? adapters.FirstOrDefault()
            ?? throw new InvalidOperationException("未找到可用的WCS API适配器 / No wcs API adapter found");

        _activeAdapterName = _activeAdapter.GetType().Name;
        
        _logger.LogInformation(
            "WCS API适配器工厂已初始化，当前激活: {AdapterName}",
            _activeAdapterName);
    }

    /// <summary>
    /// 获取当前激活的API适配器
    /// Get the currently active API adapter
    /// </summary>
    public IWcsApiAdapter GetActiveAdapter()
    {
        _logger.LogDebug("返回激活的API适配器: {AdapterName}", _activeAdapterName);
        return _activeAdapter;
    }

    /// <summary>
    /// 获取适配器类型名称
    /// Get the adapter type name
    /// </summary>
    public string GetActiveAdapterName()
    {
        return _activeAdapterName;
    }
}
