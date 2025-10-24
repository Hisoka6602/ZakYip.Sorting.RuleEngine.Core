namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 分拣机适配器接口，支持多厂商协议
/// </summary>
public interface ISorterAdapter
{
    /// <summary>
    /// 适配器名称（厂商标识）
    /// Adapter name (vendor identifier)
    /// </summary>
    string AdapterName { get; }

    /// <summary>
    /// 协议类型（TCP/Signal/HTTP等）
    /// Protocol type (TCP/Signal/HTTP, etc.)
    /// </summary>
    string ProtocolType { get; }

    /// <summary>
    /// 发送格口号到分拣机
    /// Send chute number to sorter
    /// </summary>
    /// <param name="parcelId">包裹ID / Parcel ID</param>
    /// <param name="chuteNumber">格口号 / Chute number</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>是否成功 / Success flag</returns>
    Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接状态
    /// Get connection status
    /// </summary>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
}
