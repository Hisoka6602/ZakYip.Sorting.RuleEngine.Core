using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 旺店通WMS API客户端接口
/// WDT (Wang Dian Tong) WMS API client interface
/// </summary>
public interface IWdtWmsApiClient
{
    /// <summary>
    /// 包裹称重扫描
    /// Parcel weight scanning
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="weight">重量(kg)</param>
    /// <param name="length">长度(cm)</param>
    /// <param name="width">宽度(cm)</param>
    /// <param name="height">高度(cm)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> WeighScanAsync(
        string barcode,
        decimal weight,
        decimal length,
        decimal width,
        decimal height,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询包裹信息
    /// Query parcel information
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> QueryParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传包裹图片
    /// Upload parcel image
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="imageData">图片数据</param>
    /// <param name="imageType">图片类型(如：top, side, barcode)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> UploadParcelImageAsync(
        string barcode,
        byte[] imageData,
        string imageType,
        CancellationToken cancellationToken = default);
}
