using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 规则引擎服务接口
/// </summary>
public interface IRuleEngineService
{
    /// <summary>
    /// 评估规则并返回格口号
    /// </summary>
    /// <param name="parcelInfo">包裹信息</param>
    /// <param name="dwsData">DWS数据</param>
    /// <param name="thirdPartyResponse">WCS API响应</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口号</returns>
    Task<string?> EvaluateRulesAsync(
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        WcsApiResponse? thirdPartyResponse,
        CancellationToken cancellationToken = default);
}
