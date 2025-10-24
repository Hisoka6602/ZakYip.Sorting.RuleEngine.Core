using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 规则引擎服务实现
/// Rule engine service for evaluating sorting rules with high performance
/// </summary>
public class RuleEngineService : IRuleEngineService
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ILogger<RuleEngineService> _logger;
    private IEnumerable<SortingRule>? _cachedRules;
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public RuleEngineService(
        IRuleRepository ruleRepository,
        ILogger<RuleEngineService> logger)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    /// <summary>
    /// 评估规则并返回格口号
    /// Evaluate rules and return chute number with caching for performance
    /// </summary>
    public async Task<string?> EvaluateRulesAsync(
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        ThirdPartyResponse? thirdPartyResponse,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取启用的规则（使用缓存提高性能）
            // Get enabled rules with caching for performance
            var rules = await GetCachedRulesAsync(cancellationToken);

            // 按优先级顺序评估规则
            // Evaluate rules in priority order
            foreach (var rule in rules)
            {
                if (EvaluateRule(rule, parcelInfo, dwsData, thirdPartyResponse))
                {
                    _logger.LogDebug(
                        "规则匹配成功: {RuleId} - {RuleName}, 包裹: {ParcelId}, 格口: {Chute}",
                        rule.RuleId, rule.RuleName, parcelInfo.ParcelId, rule.TargetChute);

                    return rule.TargetChute;
                }
            }

            _logger.LogWarning("未找到匹配的规则，包裹: {ParcelId}", parcelInfo.ParcelId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "规则评估失败，包裹: {ParcelId}", parcelInfo.ParcelId);
            throw;
        }
    }

    /// <summary>
    /// 获取缓存的规则列表
    /// Get cached rules list with automatic refresh
    /// </summary>
    private async Task<IEnumerable<SortingRule>> GetCachedRulesAsync(CancellationToken cancellationToken)
    {
        // 检查缓存是否过期
        // Check if cache is expired
        if (_cachedRules != null && DateTime.UtcNow - _lastCacheUpdate < _cacheExpiration)
        {
            return _cachedRules;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // 双重检查
            // Double-check after acquiring lock
            if (_cachedRules != null && DateTime.UtcNow - _lastCacheUpdate < _cacheExpiration)
            {
                return _cachedRules;
            }

            _cachedRules = await _ruleRepository.GetEnabledRulesAsync(cancellationToken);
            _lastCacheUpdate = DateTime.UtcNow;

            _logger.LogInformation("规则缓存已更新，共 {Count} 条规则", _cachedRules.Count());

            return _cachedRules;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// 评估单个规则
    /// Evaluate a single rule against parcel data
    /// </summary>
    private bool EvaluateRule(
        SortingRule rule,
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        ThirdPartyResponse? thirdPartyResponse)
    {
        try
        {
            // 简单的条件表达式评估
            // Simple condition expression evaluation
            // 支持的格式示例:
            // - "Weight > 1000" - 重量大于1000克
            // - "Volume < 50000" - 体积小于50000立方厘米
            // - "Barcode CONTAINS 'SF'" - 条码包含SF
            // - "CartNumber == 'CART001'" - 小车号等于CART001

            var condition = rule.ConditionExpression.Trim();

            // Weight条件
            if (condition.StartsWith("Weight", StringComparison.OrdinalIgnoreCase) && dwsData != null)
            {
                return EvaluateNumericCondition(condition, "Weight", dwsData.Weight);
            }

            // Volume条件
            if (condition.StartsWith("Volume", StringComparison.OrdinalIgnoreCase) && dwsData != null)
            {
                return EvaluateNumericCondition(condition, "Volume", dwsData.Volume);
            }

            // Barcode条件
            if (condition.StartsWith("Barcode", StringComparison.OrdinalIgnoreCase))
            {
                var barcode = parcelInfo.Barcode ?? dwsData?.Barcode ?? string.Empty;
                return EvaluateStringCondition(condition, "Barcode", barcode);
            }

            // CartNumber条件
            if (condition.StartsWith("CartNumber", StringComparison.OrdinalIgnoreCase))
            {
                return EvaluateStringCondition(condition, "CartNumber", parcelInfo.CartNumber);
            }

            // 默认：如果条件为"DEFAULT"或空，则匹配
            // Default: match if condition is "DEFAULT" or empty
            if (string.IsNullOrWhiteSpace(condition) || 
                condition.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "规则评估异常: {RuleId} - {Condition}", rule.RuleId, rule.ConditionExpression);
            return false;
        }
    }

    /// <summary>
    /// 评估数值条件
    /// Evaluate numeric conditions (greater than, less than, greater than or equal, less than or equal, equal)
    /// </summary>
    private bool EvaluateNumericCondition(string condition, string fieldName, decimal value)
    {
        // 使用正则表达式解析条件
        // Parse condition using regex
        var pattern = $@"{fieldName}\s*([><=]+)\s*(\d+\.?\d*)";
        var match = Regex.Match(condition, pattern, RegexOptions.IgnoreCase);

        if (!match.Success) return false;

        var op = match.Groups[1].Value.Trim();
        var threshold = decimal.Parse(match.Groups[2].Value);

        return op switch
        {
            ">" => value > threshold,
            "<" => value < threshold,
            ">=" => value >= threshold,
            "<=" => value <= threshold,
            "==" or "=" => value == threshold,
            _ => false
        };
    }

    /// <summary>
    /// 评估字符串条件
    /// Evaluate string conditions (equals, contains, starts with, ends with)
    /// </summary>
    private bool EvaluateStringCondition(string condition, string fieldName, string value)
    {
        if (condition.Contains("CONTAINS", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = $@"{fieldName}\s+CONTAINS\s+'([^']+)'";
            var match = Regex.Match(condition, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var searchValue = match.Groups[1].Value;
                return value.Contains(searchValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (condition.Contains("STARTSWITH", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = $@"{fieldName}\s+STARTSWITH\s+'([^']+)'";
            var match = Regex.Match(condition, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var searchValue = match.Groups[1].Value;
                return value.StartsWith(searchValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (condition.Contains("ENDSWITH", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = $@"{fieldName}\s+ENDSWITH\s+'([^']+)'";
            var match = Regex.Match(condition, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var searchValue = match.Groups[1].Value;
                return value.EndsWith(searchValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (condition.Contains("=="))
        {
            var pattern = $@"{fieldName}\s*==\s*'([^']+)'";
            var match = Regex.Match(condition, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var expectedValue = match.Groups[1].Value;
                return value.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }
}
