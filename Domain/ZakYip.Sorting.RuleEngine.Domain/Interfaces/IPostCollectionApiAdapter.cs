using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 邮政分揽投机构API适配器接口
/// Postal Collection/Delivery Institution API adapter interface
/// </summary>
public interface IPostCollectionApiAdapter
{
    /// <summary>
    /// 上传称重数据到邮政分揽投机构
    /// Upload weighing data to postal collection institution
    /// </summary>
    /// <param name="parcelData">包裹数据 / Parcel data</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>API响应 / API response</returns>
    Task<PostalApiResponse> UploadWeighingDataAsync(
        PostalParcelData parcelData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询包裹信息
    /// Query parcel information
    /// </summary>
    /// <param name="barcode">条码/运单号 / Barcode/Tracking number</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>API响应 / API response</returns>
    Task<PostalApiResponse> QueryParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传包裹扫描数据
    /// Upload parcel scan data
    /// </summary>
    /// <param name="barcode">条码/运单号 / Barcode/Tracking number</param>
    /// <param name="scanTime">扫描时间 / Scan time</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>API响应 / API response</returns>
    Task<PostalApiResponse> UploadScanDataAsync(
        string barcode,
        DateTime scanTime,
        CancellationToken cancellationToken = default);
}
