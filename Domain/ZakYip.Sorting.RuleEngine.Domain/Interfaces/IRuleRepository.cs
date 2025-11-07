using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Domain.Interfaces;

/// <summary>
/// 规则仓储接口
/// </summary>
public interface IRuleRepository
{
    /// <summary>
    /// 获取所有启用的规则（按优先级排序）
    /// Get all enabled rules sorted by priority
    /// </summary>
    Task<IEnumerable<SortingRule>> GetEnabledRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取规则
    /// Get rule by ID
    /// </summary>
    Task<SortingRule?> GetByIdAsync(string ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加规则
    /// Add a new rule
    /// </summary>
    Task<SortingRule> AddAsync(SortingRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新规则
    /// Update an existing rule
    /// </summary>
    Task<SortingRule> UpdateAsync(SortingRule rule, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除规则
    /// Delete a rule
    /// </summary>
    Task<bool> DeleteAsync(string ruleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有规则
    /// Get all rules
    /// </summary>
    Task<IEnumerable<SortingRule>> GetAllAsync(CancellationToken cancellationToken = default);
}
