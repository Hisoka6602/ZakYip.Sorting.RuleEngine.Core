using LiteDB;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.LiteDb;

/// <summary>
/// LiteDB规则仓储实现
/// </summary>
public class LiteDbRuleRepository : IRuleRepository
{
    private readonly ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock _clock;
    private readonly ILiteDatabase _database;
    private readonly ILogger<LiteDbRuleRepository> _logger;
    private const string CollectionName = "sorting_rules";

    public LiteDbRuleRepository(
        ILiteDatabase database,
        ILogger<LiteDbRuleRepository> logger,
        ZakYip.Sorting.RuleEngine.Domain.Interfaces.ISystemClock clock)
    {
_database = database;
        _logger = logger;
        _clock = clock;
    }

    /// <summary>
    /// 获取规则集合
    /// Get rules collection
    /// </summary>
    private ILiteCollection<SortingRule> GetCollection()
    {
        return _database.GetCollection<SortingRule>(CollectionName);
    }

    public Task<IEnumerable<SortingRule>> GetEnabledRulesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GetCollection();
            var rules = collection
                .Find(r => r.IsEnabled)
                .OrderBy(r => r.Priority)
                .ToList();

            _logger.LogDebug("获取到 {Count} 条启用的规则", rules.Count);
            return Task.FromResult<IEnumerable<SortingRule>>(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取启用规则失败");
            throw;
        }
    }

    public Task<SortingRule?> GetByIdAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GetCollection();
            var rule = collection.FindOne(r => r.RuleId == ruleId);
            return Task.FromResult<SortingRule?>(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID获取规则失败: {RuleId}", ruleId);
            throw;
        }
    }

    public Task<SortingRule> AddAsync(SortingRule rule, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GetCollection();
            rule.CreatedAt = _clock.LocalNow;
            collection.Insert(rule);
            
            _logger.LogInformation("添加规则成功: {RuleId} - {RuleName}", rule.RuleId, rule.RuleName);
            return Task.FromResult(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加规则失败: {RuleId}", rule.RuleId);
            throw;
        }
    }

    public Task<SortingRule> UpdateAsync(SortingRule rule, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GetCollection();
            rule.UpdatedAt = _clock.LocalNow;
            collection.Update(rule);
            
            _logger.LogInformation("更新规则成功: {RuleId} - {RuleName}", rule.RuleId, rule.RuleName);
            return Task.FromResult(rule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新规则失败: {RuleId}", rule.RuleId);
            throw;
        }
    }

    public Task<bool> DeleteAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GetCollection();
            var result = collection.Delete(ruleId);
            
            _logger.LogInformation("删除规则{Result}: {RuleId}", result ? "成功" : "失败", ruleId);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除规则失败: {RuleId}", ruleId);
            throw;
        }
    }

    public Task<IEnumerable<SortingRule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = GetCollection();
            var rules = collection.FindAll().OrderBy(r => r.Priority).ToList();
            
            _logger.LogDebug("获取所有规则，共 {Count} 条", rules.Count);
            return Task.FromResult<IEnumerable<SortingRule>>(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有规则失败");
            throw;
        }
    }
}
