using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Managers;

/// <summary>
/// 适配器管理器 - 支持热切换
/// </summary>
public class AdapterManager<T> : IAdapterManager<T> where T : class
{
    private readonly ILogger<AdapterManager<T>> _logger;
    private readonly Dictionary<string, T> _adapters;
    private T _activeAdapter;
    private string _activeAdapterName;
    private readonly SemaphoreSlim _switchLock = new(1, 1);

    public AdapterManager(
        IEnumerable<T> adapters,
        string defaultAdapterName,
        ILogger<AdapterManager<T>> logger)
    {
        _logger = logger;
        _adapters = new Dictionary<string, T>();

        // 注册所有适配器
        foreach (var adapter in adapters)
        {
            var name = GetAdapterName(adapter);
            _adapters[name] = adapter;
        }

        if (_adapters.Count == 0)
        {
            throw new InvalidOperationException($"未找到任何 {typeof(T).Name} 适配器");
        }

        // 设置默认适配器
        if (!_adapters.TryGetValue(defaultAdapterName, out var defaultAdapter))
        {
            // 如果找不到默认适配器，使用第一个
            defaultAdapter = _adapters.First().Value;
            defaultAdapterName = _adapters.First().Key;
            _logger.LogWarning("未找到默认适配器 {DefaultName}，使用 {ActualName}", 
                defaultAdapterName, _adapters.First().Key);
        }

        _activeAdapter = defaultAdapter;
        _activeAdapterName = defaultAdapterName;
        _logger.LogInformation("适配器管理器已初始化，当前活动适配器: {AdapterName}", _activeAdapterName);
    }

    /// <summary>
    /// 获取当前活动的适配器
    /// </summary>
    public T GetActiveAdapter()
    {
        return _activeAdapter;
    }

    /// <summary>
    /// 切换到指定适配器
    /// </summary>
    public async Task SwitchAdapterAsync(string adapterName, CancellationToken cancellationToken = default)
    {
        await _switchLock.WaitAsync(cancellationToken);
        try
        {
            if (!_adapters.TryGetValue(adapterName, out var newAdapter))
            {
                throw new InvalidOperationException($"未找到适配器: {adapterName}");
            }

            if (_activeAdapterName == adapterName)
            {
                _logger.LogInformation("适配器已经是 {AdapterName}，无需切换", adapterName);
                return;
            }

            var oldAdapterName = _activeAdapterName;

            // 停止旧适配器（如果实现了IDisposable）
            if (_activeAdapter is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "停止旧适配器时发生错误: {OldAdapter}", oldAdapterName);
                }
            }

            // 切换到新适配器
            _activeAdapter = newAdapter;
            _activeAdapterName = adapterName;

            _logger.LogInformation("适配器已切换: {OldAdapter} -> {NewAdapter}", oldAdapterName, adapterName);
        }
        finally
        {
            _switchLock.Release();
        }
    }

    /// <summary>
    /// 获取所有可用适配器
    /// </summary>
    public IEnumerable<T> GetAllAdapters()
    {
        return _adapters.Values;
    }

    /// <summary>
    /// 获取当前活动适配器名称
    /// </summary>
    public string GetActiveAdapterName()
    {
        return _activeAdapterName;
    }

    private string GetAdapterName(T adapter)
    {
        // 尝试通过反射获取AdapterName属性
        var property = adapter.GetType().GetProperty("AdapterName");
        if (property != null)
        {
            return property.GetValue(adapter)?.ToString() ?? adapter.GetType().Name;
        }
        return adapter.GetType().Name;
    }
}
