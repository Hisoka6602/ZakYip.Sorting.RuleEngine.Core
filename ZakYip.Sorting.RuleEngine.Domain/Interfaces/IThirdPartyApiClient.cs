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

    /// <summary>
    /// 扫描包裹
    /// Scan parcel to register it in the third-party system
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求格口
    /// Request a chute/gate number for the parcel
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应，包含格口号</returns>
    Task<ThirdPartyResponse> RequestChuteAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传图片
    /// Upload image to third-party API
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="imageData">图片数据（字节数组）</param>
    /// <param name="contentType">图片内容类型（例如：image/jpeg, image/png）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = "image/jpeg",
        CancellationToken cancellationToken = default);
}
