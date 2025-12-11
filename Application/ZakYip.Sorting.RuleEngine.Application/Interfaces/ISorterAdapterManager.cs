using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Interfaces;

/// <summary>
/// 分拣机适配器管理器接口
/// Sorter adapter manager interface
/// </summary>
public interface ISorterAdapterManager : IAdapterManager<SorterConfig>
{
    /// <summary>
    /// 发送格口号到分拣机
    /// Send chute number to sorter
    /// </summary>
    Task<bool> SendChuteNumberAsync(string parcelId, string chuteNumber, CancellationToken cancellationToken = default);
}
