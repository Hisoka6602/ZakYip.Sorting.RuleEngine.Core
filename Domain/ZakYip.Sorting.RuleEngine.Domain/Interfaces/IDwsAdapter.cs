using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// DWS适配器接口
/// </summary>
public interface IDwsAdapter
{
    /// <summary>
    /// 适配器名称
    /// </summary>
    string AdapterName { get; }
    
    /// <summary>
    /// 协议类型
    /// </summary>
    string ProtocolType { get; }
    
    /// <summary>
    /// 启动DWS监听
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 停止DWS监听
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// DWS数据接收事件
    /// </summary>
    event Func<DwsData, Task>? OnDwsDataReceived;
}
