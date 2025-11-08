using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 规则仓储接口
/// </summary>
public interface IRuleRepository
{
    /// <summary>
    /// 获取所有启用的规则（按优先级排序）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按优先级排序的启用规则集合</returns>
    Task<IEnumerable<SortingRule>> GetEnabledRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取规则
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>指定ID的规则，如果不存在则返回null</returns>
    Task<SortingRule?> GetByIdAsync(string ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加规则
    /// </summary>
    /// <param name="rule">要添加的规则实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加成功的规则实体</returns>
    Task<SortingRule> AddAsync(SortingRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新规则
    /// </summary>
    /// <param name="rule">要更新的规则实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的规则实体</returns>
    Task<SortingRule> UpdateAsync(SortingRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除规则
    /// </summary>
    /// <param name="ruleId">要删除的规则ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除成功返回true，否则返回false</returns>
    Task<bool> DeleteAsync(string ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有规则
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有规则的集合</returns>
    Task<IEnumerable<SortingRule>> GetAllAsync(CancellationToken cancellationToken = default);
}
