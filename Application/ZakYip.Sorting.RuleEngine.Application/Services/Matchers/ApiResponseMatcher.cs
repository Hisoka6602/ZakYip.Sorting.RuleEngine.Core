using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// API响应内容匹配器
/// 支持字符串查找、正则查找、JSON匹配
/// </summary>
public class ApiResponseMatcher
{
    /// <summary>
    /// 评估API响应匹配（使用枚举）
    /// </summary>
    /// <param name="matchType">API响应匹配类型（字符串匹配、反向字符串匹配、正则匹配、JSON匹配）</param>
    /// <param name="parameter">匹配参数（根据匹配类型而定，如关键字、正则模式、JSON路径等）</param>
    /// <param name="responseData">API响应数据字符串</param>
    /// <returns>如果响应数据符合匹配条件返回true，否则返回false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Evaluate(ApiResponseMatchType matchType, string parameter, string? responseData)
    {
        if (string.IsNullOrWhiteSpace(responseData) || string.IsNullOrWhiteSpace(parameter))
            return false;

        try
        {
            return matchType switch
            {
                ApiResponseMatchType.String => responseData.Contains(parameter, StringComparison.OrdinalIgnoreCase),
                ApiResponseMatchType.StringReverse => responseData.LastIndexOf(parameter, StringComparison.OrdinalIgnoreCase) >= 0,
                ApiResponseMatchType.Regex => EvaluateRegex(parameter, responseData),
                ApiResponseMatchType.Json => EvaluateJsonMatch(parameter, responseData),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 评估API响应匹配表达式（保持向后兼容）
    /// 支持多种格式：
    /// - STRING:keyword (正向字符串查找)
    /// - STRING_REVERSE:keyword (反向字符串查找)
    /// - REGEX:pattern (正则表达式匹配)
    /// - JSON:field=value (JSON字段精确匹配)
    /// - JSON:field.nested=value (JSON嵌套字段匹配)
    /// </summary>
    /// <param name="expression">匹配表达式字符串</param>
    /// <param name="responseData">API响应数据字符串</param>
    /// <returns>如果响应数据符合表达式规则返回true，否则返回false</returns>
    public bool Evaluate(string expression, string? responseData)
    {
        if (string.IsNullOrWhiteSpace(expression) || string.IsNullOrWhiteSpace(responseData))
            return false;

        try
        {
            var expr = expression.Trim();

            // 字符串查找（正向）
            if (expr.StartsWith("STRING:", StringComparison.OrdinalIgnoreCase))
            {
                var keyword = expr.Substring("STRING:".Length).Trim();
                return Evaluate(ApiResponseMatchType.String, keyword, responseData);
            }

            // 字符串查找（反向）
            if (expr.StartsWith("STRING_REVERSE:", StringComparison.OrdinalIgnoreCase))
            {
                var keyword = expr.Substring("STRING_REVERSE:".Length).Trim();
                return Evaluate(ApiResponseMatchType.StringReverse, keyword, responseData);
            }

            // 正则查找
            if (expr.StartsWith("REGEX:", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = expr.Substring("REGEX:".Length).Trim();
                return Evaluate(ApiResponseMatchType.Regex, pattern, responseData);
            }

            // JSON匹配
            if (expr.StartsWith("JSON:", StringComparison.OrdinalIgnoreCase))
            {
                var jsonExpr = expr.Substring("JSON:".Length).Trim();
                return Evaluate(ApiResponseMatchType.Json, jsonExpr, responseData);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 评估正则表达式匹配
    /// </summary>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="responseData">要匹配的响应数据</param>
    /// <returns>如果响应数据匹配正则表达式返回true，否则返回false</returns>
    private bool EvaluateRegex(string pattern, string responseData)
    {
        return Regex.IsMatch(responseData, pattern);
    }

    /// <summary>
    /// 评估JSON字段匹配
    /// </summary>
    /// <param name="expression">JSON匹配表达式，格式为"field=value"或"field.nested=value"</param>
    /// <param name="jsonData">JSON格式的响应数据</param>
    /// <returns>如果JSON数据中指定字段的值与期望值匹配返回true，否则返回false</returns>
    private bool EvaluateJsonMatch(string expression, string jsonData)
    {
        try
        {
            // 解析表达式：field=value 或 field.nested=value
            var parts = expression.Split('=', 2);
            if (parts.Length != 2)
                return false;

            var fieldPath = parts[0].Trim();
            var expectedValue = parts[1].Trim();

            // 解析JSON
            using var document = JsonDocument.Parse(jsonData);
            var root = document.RootElement;

            // 获取字段值
            var actualValue = GetJsonValue(root, fieldPath);
            if (actualValue == null)
                return false;

            // 比较值
            return actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从JSON元素中获取指定路径的值
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <param name="fieldPath">字段路径，使用点号分隔（如"user.name"）</param>
    /// <returns>找到字段时返回字段值的字符串表示，否则返回null</returns>
    private string? GetJsonValue(JsonElement element, string fieldPath)
    {
        try
        {
            var parts = fieldPath.Split('.');
            var current = element;

            foreach (var part in parts)
            {
                if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var property))
                {
                    current = property;
                }
                else
                {
                    return null;
                }
            }

            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.GetDecimal().ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => null,
                _ => current.GetRawText()
            };
        }
        catch
        {
            return null;
        }
    }
}
