using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 下游通信工厂接口
/// Downstream communication factory interface
/// </summary>
/// <remarks>
/// 负责根据配置动态创建下游通信实例（Server或Client模式）
/// Responsible for dynamically creating downstream communication instances based on configuration (Server or Client mode)
/// </remarks>
public interface IDownstreamCommunicationFactory
{
    /// <summary>
    /// 根据配置创建下游通信实例
    /// Create downstream communication instance based on configuration
    /// </summary>
    /// <param name="config">分拣机配置 / Sorter configuration</param>
    /// <returns>下游通信实例 / Downstream communication instance</returns>
    IDownstreamCommunication Create(SorterConfig? config);
}
