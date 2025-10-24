namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 适配器管理器接口 - 支持热切换
/// </summary>
/// <typeparam name="T">适配器类型</typeparam>
public interface IAdapterManager<T> where T : class
{
    /// <summary>
    /// 获取当前活动的适配器
    /// </summary>
    T GetActiveAdapter();
    
    /// <summary>
    /// 切换到指定适配器
    /// </summary>
    Task SwitchAdapterAsync(string adapterName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取所有可用适配器
    /// </summary>
    IEnumerable<T> GetAllAdapters();
    
    /// <summary>
    /// 获取当前活动适配器名称
    /// </summary>
    string GetActiveAdapterName();
}
