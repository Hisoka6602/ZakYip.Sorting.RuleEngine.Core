namespace ZakYip.Sorting.RuleEngine.Application.Interfaces;

/// <summary>
/// 适配器管理器基接口 - 提供连接管理的通用功能
/// Base adapter manager interface - Provides common connection management functionality
/// </summary>
/// <typeparam name="TConfig">配置类型 / Configuration type</typeparam>
/// <remarks>
/// 此接口提供适配器管理的通用功能。
/// 遵循DRY原则。
/// This interface provides common adapter management functionality, 
/// following the DRY principle.
/// </remarks>
public interface IAdapterManager<TConfig> where TConfig : class
{
    /// <summary>
    /// 使用新配置连接适配器
    /// Connect to adapter with new configuration
    /// </summary>
    /// <param name="config">配置对象 / Configuration object</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    Task ConnectAsync(TConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开适配器连接
    /// Disconnect from adapter
    /// </summary>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接状态
    /// Get connection status
    /// </summary>
    bool IsConnected { get; }
}
