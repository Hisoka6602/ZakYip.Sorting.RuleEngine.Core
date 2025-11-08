using ZakYip.Sorting.RuleEngine.Domain.Constants;
using ZakYip.Sorting.RuleEngine.Domain.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 增强版WCS API适配器接口
/// Enhanced WCS API adapter interface with strongly-typed responses and batch operations
/// </summary>
public interface IEnhancedWcsApiAdapter : IWcsApiAdapter
{
    /// <summary>
    /// 扫描包裹（强类型响应）
    /// Scan parcel with strongly-typed response
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>强类型API响应</returns>
    Task<StronglyTypedApiResponseDto<ScanParcelResponseData>> ScanParcelStronglyTypedAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求格口（强类型响应）
    /// Request chute with strongly-typed response
    /// </summary>
    /// <param name="parcelId">包裹ID</param>
    /// <param name="dwsData">DWS数据</param>
    /// <param name="ocrData">OCR数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>强类型API响应</returns>
    Task<StronglyTypedApiResponseDto<ChuteRequestResponseData>> RequestChuteStronglyTypedAsync(
        string parcelId,
        DwsData dwsData,
        OcrData? ocrData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传图片（强类型响应）
    /// Upload image with strongly-typed response
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="imageData">图片数据</param>
    /// <param name="contentType">图片内容类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>强类型API响应</returns>
    Task<StronglyTypedApiResponseDto<ImageUploadResponseData>> UploadImageStronglyTypedAsync(
        string barcode,
        byte[] imageData,
        string contentType = ConfigurationDefaults.ImageFile.DefaultContentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量请求格口
    /// Batch request chutes for multiple parcels
    /// </summary>
    /// <param name="requests">批量请求数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    Task<BatchOperationResponse<WcsApiResponse>> BatchRequestChuteAsync(
        BatchOperationRequest<(string ParcelId, DwsData DwsData, OcrData? OcrData)> requests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量上传图片
    /// Batch upload images
    /// </summary>
    /// <param name="requests">批量请求数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    Task<BatchOperationResponse<WcsApiResponse>> BatchUploadImageAsync(
        BatchOperationRequest<(string Barcode, byte[] ImageData, string ContentType)> requests,
        CancellationToken cancellationToken = default);
}
