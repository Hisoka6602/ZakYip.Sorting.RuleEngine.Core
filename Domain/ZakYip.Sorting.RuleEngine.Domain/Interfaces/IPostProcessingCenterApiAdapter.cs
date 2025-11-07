using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 邮政处理中心API适配器接口
/// Postal Processing Center API adapter interface
/// </summary>
public interface IPostProcessingCenterApiAdapter
{
    /// <summary>
    /// 上传称重数据到邮政处理中心
    /// Upload weighing data to postal processing center
    /// </summary>
    /// <param name="parcelData">包裹数据 / Parcel data</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>API响应 / API response</returns>
    Task<PostalApiResponse> UploadWeighingDataAsync(
        PostalParcelData parcelData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询包裹路由信息
    /// Query parcel routing information
    /// </summary>
    /// <param name="barcode">条码/运单号 / Barcode/Tracking number</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>API响应 / API response</returns>
    Task<PostalApiResponse> QueryParcelRoutingAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传分拣结果
    /// Upload sorting result
    /// </summary>
    /// <param name="barcode">条码/运单号 / Barcode/Tracking number</param>
    /// <param name="destinationCode">目的地代码 / Destination code</param>
    /// <param name="chuteNumber">格口号 / Chute number</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>API响应 / API response</returns>
    Task<PostalApiResponse> UploadSortingResultAsync(
        string barcode,
        string destinationCode,
        string chuteNumber,
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
