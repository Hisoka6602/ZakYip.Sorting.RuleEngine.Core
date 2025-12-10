using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Interfaces;

/// <summary>
/// 分拣机适配器管理器接口
/// Sorter adapter manager interface
/// </summary>
public interface ISorterAdapterManager
{
    /// <summary>
    /// 使用新配置连接分拣机
    /// Connect to sorter with new configuration
    /// </summary>
    Task ConnectAsync(SorterConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开分拣机连接
    /// Disconnect from sorter
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接状态
    /// Get connection status
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// 发送格口号到分拣机
    /// Send chute number to sorter
    /// </summary>
    Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default);
}
