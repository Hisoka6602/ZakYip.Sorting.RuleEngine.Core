using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 第三方API客户端接口
/// </summary>
public interface IThirdPartyApiClient
{
    /// <summary>
    /// 上传包裹和DWS数据到第三方API
    /// Upload parcel and DWS data to third-party API
    /// </summary>
    /// <param name="parcelInfo">包裹信息</param>
    /// <param name="dwsData">DWS数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> UploadDataAsync(
        ParcelInfo parcelInfo,
        DwsData dwsData,
        CancellationToken cancellationToken = default);
}
