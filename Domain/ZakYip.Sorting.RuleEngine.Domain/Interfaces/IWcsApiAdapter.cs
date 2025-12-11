using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// WCS API适配器接口
/// WCS API adapter interface
/// </summary>
public interface IWcsApiAdapter
{
    /// <summary>
    /// 扫描包裹
    /// Scan parcel to register it in the wcs system
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>WCS API响应</returns>
    Task<WcsApiResponse> ScanParcelAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求格口（对应参考代码的UploadData方法）
    /// Request a chute/gate number for the parcel
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="dwsData">DWS数据（重量、尺寸等）</param>
    /// <param name="ocrData">OCR数据（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>WCS API响应，包含格口号</returns>
    Task<WcsApiResponse> RequestChuteAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传图片
    /// Upload image to wcs API
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="imageData">图片数据（字节数组）</param>
    /// <param name="contentType">图片内容类型（例如：image/jpeg, image/png）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>WCS API响应</returns>
    Task<WcsApiResponse> UploadImageAsync(
        string barcode,
        byte[] imageData,
        string contentType = ImageFileDefaults.DefaultContentType,
        CancellationToken cancellationToken = default);
}
