using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients;

/// <summary>
/// WCS API适配器工厂实现
/// WCS API adapter factory implementation
/// 根据配置和自动应答模式选择激活的API适配器
/// Selects the active API adapter based on configuration and auto-response mode
/// </summary>
public class WcsApiAdapterFactory : IWcsApiAdapterFactory
{
    private readonly IWcsApiAdapter _configuredAdapter;
    private readonly IWcsApiAdapter _mockAdapter;
    private readonly IAutoResponseModeService _autoResponseModeService;
    private readonly string _configuredAdapterName;
    private readonly ILogger<WcsApiAdapterFactory> _logger;

    public WcsApiAdapterFactory(
        IEnumerable<IWcsApiAdapter> adapters,
        string activeAdapterType,
        IAutoResponseModeService autoResponseModeService,
        ILogger<WcsApiAdapterFactory> logger)
    {
        _logger = logger;
        _autoResponseModeService = autoResponseModeService;
        
        // 查找模拟适配器
        // Find mock adapter
        _mockAdapter = adapters.FirstOrDefault(a => a is MockWcsApiAdapter)
            ?? throw new InvalidOperationException("未找到模拟WCS API适配器 / Mock WCS API adapter not found");
        
        // 根据配置选择激活的适配器（排除模拟适配器）
        // Select the active adapter based on configuration (excluding mock adapter)
        _configuredAdapter = adapters
            .Where(a => a is not MockWcsApiAdapter)
            .FirstOrDefault(a => a.GetType().Name == activeAdapterType)
            ?? adapters.FirstOrDefault(a => a is not MockWcsApiAdapter)
            ?? throw new InvalidOperationException("未找到可用的WCS API适配器 / No WCS API adapter found");

        _configuredAdapterName = _configuredAdapter.GetType().Name;
        
        _logger.LogInformation(
            "WCS API适配器工厂已初始化，配置适配器: {AdapterName}",
            _configuredAdapterName);
    }

    /// <summary>
    /// 获取当前激活的API适配器
    /// Get the currently active API adapter
    /// 如果自动应答模式启用，返回模拟适配器；否则返回配置的适配器
    /// Returns mock adapter if auto-response mode is enabled, otherwise returns configured adapter
    /// </summary>
    public IWcsApiAdapter GetActiveAdapter()
    {
        if (_autoResponseModeService.IsEnabled)
        {
            _logger.LogDebug("自动应答模式已启用，使用模拟适配器");
            return _mockAdapter;
        }

        _logger.LogDebug("返回配置的API适配器: {AdapterName}", _configuredAdapterName);
        return _configuredAdapter;
    }

    /// <summary>
    /// 获取适配器类型名称
    /// Get the adapter type name
    /// </summary>
    public string GetActiveAdapterName()
    {
        return _autoResponseModeService.IsEnabled 
            ? "MockWcsApiAdapter" 
            : _configuredAdapterName;
    }
}
