using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// WCS API适配器工厂接口
/// WCS API adapter factory interface
/// </summary>
public interface IWcsApiAdapterFactory
{
    /// <summary>
    /// 获取当前激活的API适配器
    /// Get the currently active API adapter
    /// </summary>
    /// <returns>激活的API适配器实例</returns>
    IWcsApiAdapter GetActiveAdapter();

    /// <summary>
    /// 获取适配器类型名称
    /// Get the adapter type name
    /// </summary>
    /// <returns>适配器类型名称</returns>
    string GetActiveAdapterName();

    /// <summary>
    /// 清空缓存，强制下次调用时重新加载配置（用于热更新）
    /// Clear cache to force reload on next call (for hot reload)
    /// </summary>
    void InvalidateCache();
}
