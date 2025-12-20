namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 适配器热切换管理器接口 - 支持运行时切换不同适配器实现
/// Adapter hot-swap manager interface - Supports runtime switching between different adapter implementations
/// </summary>
/// <typeparam name="T">适配器类型 / Adapter type</typeparam>
/// <remarks>
/// 此接口专注于适配器的热切换功能，与Application.Interfaces.IAdapterManager（连接管理）职责不同。
/// This interface focuses on hot-swapping functionality, different from Application.Interfaces.IAdapterManager (connection management).
/// </remarks>
public interface IAdapterSwitchManager<T> where T : class
{
    /// <summary>
    /// 获取当前活动的适配器
    /// Get currently active adapter
    /// </summary>
    T GetActiveAdapter();
    
    /// <summary>
    /// 切换到指定适配器
    /// Switch to specified adapter
    /// </summary>
    Task SwitchAdapterAsync(string adapterName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取所有可用适配器
    /// Get all available adapters
    /// </summary>
    IEnumerable<T> GetAllAdapters();
    
    /// <summary>
    /// 获取当前活动适配器名称
    /// Get active adapter name
    /// </summary>
    string GetActiveAdapterName();
}
