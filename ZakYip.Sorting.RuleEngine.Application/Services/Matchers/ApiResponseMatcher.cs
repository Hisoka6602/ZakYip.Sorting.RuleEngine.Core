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
    /// 格式示例：
    /// - STRING:keyword (正向字符串查找)
    /// - STRING_REVERSE:keyword (反向字符串查找)
    /// - REGEX:pattern (正则查找)
    /// - JSON:field=value (JSON字段匹配)
    /// - JSON:field.nested=value (JSON嵌套字段匹配)
    /// </summary>
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
    /// 评估正则表达式
    /// </summary>
    private bool EvaluateRegex(string pattern, string responseData)
    {
        return Regex.IsMatch(responseData, pattern);
    }

    /// <summary>
    /// 评估JSON匹配
    /// </summary>
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
