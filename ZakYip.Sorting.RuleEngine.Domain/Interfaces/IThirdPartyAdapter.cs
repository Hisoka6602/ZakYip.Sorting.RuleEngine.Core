using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// WCS API适配器接口，支持多厂商协议
/// </summary>
public interface IWcsAdapter
{
    /// <summary>
    /// 适配器名称（厂商标识）
    /// Adapter name (vendor identifier)
    /// </summary>
    string AdapterName { get; }

    /// <summary>
    /// 协议类型（HTTP/TCP等）
    /// Protocol type (HTTP/TCP, etc.)
    /// </summary>
    string ProtocolType { get; }

    /// <summary>
    /// 调用WCS API
    /// Call wcs API
    /// </summary>
    /// <param name="parcelInfo">包裹信息 / Parcel information</param>
    /// <param name="dwsData">DWS数据 / DWS data</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>第三方响应 / WCS response</returns>
    Task<WcsApiResponse> CallApiAsync(ParcelInfo parcelInfo, DwsData dwsData, CancellationToken cancellationToken = default);
}
