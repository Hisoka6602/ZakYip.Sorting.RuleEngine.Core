using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 聚水潭ERP API客户端接口
/// Jushuituan ERP API client interface
/// </summary>
public interface IJushuitanErpApiClient
{
    /// <summary>
    /// 称重回传
    /// Weight data callback
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="weight">重量(kg)</param>
    /// <param name="length">长度(cm)</param>
    /// <param name="width">宽度(cm)</param>
    /// <param name="height">高度(cm)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> WeightCallbackAsync(
        string barcode,
        decimal weight,
        decimal length,
        decimal width,
        decimal height,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询订单信息
    /// Query order information
    /// </summary>
    /// <param name="barcode">包裹条码或订单号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> QueryOrderAsync(
        string barcode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新物流信息
    /// Update logistics information
    /// </summary>
    /// <param name="barcode">包裹条码</param>
    /// <param name="logisticsCompany">物流公司</param>
    /// <param name="trackingNumber">物流单号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方API响应</returns>
    Task<ThirdPartyResponse> UpdateLogisticsAsync(
        string barcode,
        string logisticsCompany,
        string trackingNumber,
        CancellationToken cancellationToken = default);
}
