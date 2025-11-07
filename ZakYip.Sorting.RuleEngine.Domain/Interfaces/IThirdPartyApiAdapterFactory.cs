using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 第三方API适配器工厂接口
/// Third-party API adapter factory interface
/// </summary>
public interface IThirdPartyApiAdapterFactory
{
    /// <summary>
    /// 获取当前激活的API适配器
    /// Get the currently active API adapter
    /// </summary>
    /// <returns>激活的API适配器实例</returns>
    IThirdPartyApiAdapter GetActiveAdapter();

    /// <summary>
    /// 获取适配器类型名称
    /// Get the adapter type name
    /// </summary>
    /// <returns>适配器类型名称</returns>
    string GetActiveAdapterName();
}
