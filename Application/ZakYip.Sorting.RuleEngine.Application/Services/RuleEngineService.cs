using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Interfaces;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

namespace ZakYip.Sorting.RuleEngine.Application.Services;

/// <summary>
/// 规则引擎服务实现
/// </summary>
public class RuleEngineService : IRuleEngineService
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ILogger<RuleEngineService> _logger;
    private readonly IMemoryCache _cache;
    private readonly PerformanceMetricService _performanceService;
    private const string CacheKey = "SortingRules";
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    // 新增匹配器
    private readonly BarcodeRegexMatcher _barcodeMatcher = new();
    private readonly WeightMatcher _weightMatcher = new();
    private readonly VolumeMatcher _volumeMatcher = new();
    private readonly OcrMatcher _ocrMatcher = new();
    private readonly ApiResponseMatcher _apiResponseMatcher = new();
    private readonly LowCodeExpressionMatcher _lowCodeMatcher = new();

    public RuleEngineService(
        IRuleRepository ruleRepository,
        ILogger<RuleEngineService> logger,
        IMemoryCache cache,
        PerformanceMetricService performanceService)
    {
        _ruleRepository = ruleRepository;
        _logger = logger;
        _cache = cache;
        _performanceService = performanceService;
    }

    /// <summary>
    /// 评估规则并返回格口号（支持多规则匹配）
    /// </summary>
    public async Task<string?> EvaluateRulesAsync(
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        WcsApiResponse? thirdPartyResponse,
        CancellationToken cancellationToken = default)
    {
        return await _performanceService.ExecuteWithMetricsAsync(
            "RuleEvaluation",
            async () =>
            {
                try
                {
                    // 获取启用的规则（使用缓存提高性能）
                    var rules = await GetCachedRulesAsync(cancellationToken);

                    // 收集所有匹配的规则
                    var matchedRules = new List<SortingRule>();

                    // 按优先级顺序评估规则
                    foreach (var rule in rules)
                    {
                        if (EvaluateRule(rule, parcelInfo, dwsData, thirdPartyResponse))
                        {
                            matchedRules.Add(rule);
                            _logger.LogDebug(
                                "规则匹配成功: {RuleId} - {RuleName}, 包裹: {ParcelId}, 格口: {Chute}",
                                rule.RuleId, rule.RuleName, parcelInfo.ParcelId, rule.TargetChute);
                        }
                    }

                    // 如果有匹配的规则，返回优先级最高的（第一个匹配的）
                    if (matchedRules.Any())
                    {
                        var selectedRule = matchedRules.First();
                        _logger.LogInformation(
                            "包裹 {ParcelId} 匹配到 {Count} 条规则，选择优先级最高的规则: {RuleId}, 格口: {Chute}",
                            parcelInfo.ParcelId, matchedRules.Count, selectedRule.RuleId, selectedRule.TargetChute);
                        return selectedRule.TargetChute;
                    }

                    _logger.LogWarning("未找到匹配的规则，包裹: {ParcelId}", parcelInfo.ParcelId);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "规则评估失败，包裹: {ParcelId}", parcelInfo.ParcelId);
                    throw;
                }
            },
            parcelInfo.ParcelId,
            null,
            cancellationToken);
    }

    /// <summary>
    /// 获取缓存的规则列表（使用滑动过期缓存）
    /// </summary>
    private async Task<IEnumerable<SortingRule>> GetCachedRulesAsync(CancellationToken cancellationToken)
    {
        // 尝试从缓存获取
        if (_cache.TryGetValue(CacheKey, out IEnumerable<SortingRule>? cachedRules) && cachedRules != null)
        {
            return cachedRules;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // 双重检查
            if (_cache.TryGetValue(CacheKey, out cachedRules) && cachedRules != null)
            {
                return cachedRules;
            }

            // 从数据库加载规则
            var rules = await _ruleRepository.GetEnabledRulesAsync(cancellationToken);

            // 配置滑动过期缓存选项
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))  // 滑动过期5分钟
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30)) // 绝对过期30分钟
                .SetPriority(CacheItemPriority.High) // 高优先级，避免被驱逐
                .RegisterPostEvictionCallback((key, value, reason, state) =>
                {
                    _logger.LogInformation("规则缓存被移除，原因: {Reason}", reason);
                });

            // 存入缓存
            _cache.Set(CacheKey, rules, cacheOptions);

            _logger.LogInformation("规则缓存已更新，共 {Count} 条规则", rules.Count());

            return rules;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// 手动清除缓存（配置更新时调用）
    /// </summary>
    public void ClearCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("规则缓存已手动清除");
    }

    /// <summary>
    /// 评估单个规则
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluateRule(
        SortingRule rule,
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        WcsApiResponse? thirdPartyResponse)
    {
        try
        {
            var condition = rule.ConditionExpression.Trim();

            // 根据匹配方法类型选择不同的评估器
            switch (rule.MatchingMethod)
            {
                case MatchingMethodType.BarcodeRegex:
                    var barcode = parcelInfo.Barcode ?? dwsData?.Barcode ?? string.Empty;
                    return _barcodeMatcher.Evaluate(condition, barcode);

                case MatchingMethodType.WeightMatch:
                    if (dwsData != null)
                    {
                        return _weightMatcher.Evaluate(condition, dwsData.Weight);
                    }
                    return false;

                case MatchingMethodType.VolumeMatch:
                    if (dwsData != null)
                    {
                        return _volumeMatcher.Evaluate(condition, dwsData);
                    }
                    return false;

                case MatchingMethodType.OcrMatch:
                    if (thirdPartyResponse?.OcrData != null)
                    {
                        return _ocrMatcher.Evaluate(condition, thirdPartyResponse.OcrData);
                    }
                    return false;

                case MatchingMethodType.ApiResponseMatch:
                    if (thirdPartyResponse?.Data != null)
                    {
                        return _apiResponseMatcher.Evaluate(condition, thirdPartyResponse.Data);
                    }
                    return false;

                case MatchingMethodType.LowCodeExpression:
                    return _lowCodeMatcher.Evaluate(condition, parcelInfo, dwsData, thirdPartyResponse);

                case MatchingMethodType.LegacyExpression:
                default:
                    // 使用传统的评估方法（兼容旧版）
                    return EvaluateLegacyExpression(condition, parcelInfo, dwsData, thirdPartyResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "规则评估异常: {RuleId} - {Condition}", rule.RuleId, rule.ConditionExpression);
            return false;
        }
    }

    /// <summary>
    /// 评估传统表达式（兼容旧版）
    /// </summary>
    private bool EvaluateLegacyExpression(
        string condition,
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        WcsApiResponse? thirdPartyResponse)
    {
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
        if (string.IsNullOrWhiteSpace(condition) ||
            condition.Equals("DEFAULT", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 评估数值条件
    /// </summary>
    private bool EvaluateNumericCondition(string condition, string fieldName, decimal value)
    {
        // 使用正则表达式解析条件
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
    /// </summary>
    private bool EvaluateStringCondition(string condition, string fieldName, string value)
    {
        if (condition.Contains("CONTAINS", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = $@"{fieldName}\s+CONTAINS\s+'([^']*)'";
            var match = Regex.Match(condition, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var searchValue = match.Groups[1].Value;
                return value.Contains(searchValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (condition.Contains("STARTSWITH", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = $@"{fieldName}\s+STARTSWITH\s+'([^']*)'";
            var match = Regex.Match(condition, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var searchValue = match.Groups[1].Value;
                return value.StartsWith(searchValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (condition.Contains("ENDSWITH", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = $@"{fieldName}\s+ENDSWITH\s+'([^']*)'";
            var match = Regex.Match(condition, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var searchValue = match.Groups[1].Value;
                return value.EndsWith(searchValue, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (condition.Contains("=="))
        {
            var pattern = $@"{fieldName}\s*==\s*'([^']*)'";
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
