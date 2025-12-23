namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 可重载的下游通信接口（支持配置热更新）
/// Reloadable downstream communication interface (supports configuration hot reload)
/// </summary>
/// <remarks>
/// 此接口扩展 IDownstreamCommunication，添加配置热更新能力
/// This interface extends IDownstreamCommunication and adds configuration hot reload capability
/// </remarks>
public interface IReloadableDownstreamCommunication : IDownstreamCommunication
{
    /// <summary>
    /// 重新加载配置（配置变更时调用）
    /// Reload configuration (called when configuration changes)
    /// </summary>
    /// <remarks>
    /// 执行流程：
    /// 1. 加载新配置
    /// 2. 停止旧实例
    /// 3. 创建新实例
    /// 4. 如果新配置启用，启动新实例
    /// 5. 释放旧实例资源
    /// 
    /// Execution flow:
    /// 1. Load new configuration
    /// 2. Stop old instance
    /// 3. Create new instance
    /// 4. If new config is enabled, start new instance
    /// 5. Dispose old instance resources
    /// </remarks>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
