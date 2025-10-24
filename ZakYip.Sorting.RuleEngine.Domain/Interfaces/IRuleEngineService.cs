using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 规则引擎服务接口
/// Rule engine service interface for evaluating sorting rules
/// </summary>
public interface IRuleEngineService
{
    /// <summary>
    /// 评估规则并返回格口号
    /// Evaluate rules and return chute number
    /// </summary>
    /// <param name="parcelInfo">包裹信息</param>
    /// <param name="dwsData">DWS数据</param>
    /// <param name="thirdPartyResponse">第三方API响应</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>格口号</returns>
    Task<string?> EvaluateRulesAsync(
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        ThirdPartyResponse? thirdPartyResponse,
        CancellationToken cancellationToken = default);
}
