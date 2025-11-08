using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 条码正则匹配器
/// </summary>
public class BarcodeRegexMatcher
{
    /// <summary>
    /// 评估条码正则匹配（使用枚举）
    /// </summary>
    /// <param name="preset">条码匹配预设类型</param>
    /// <param name="parameter">匹配参数（根据预设类型而定，如前缀、子串、长度范围等）</param>
    /// <param name="barcode">要匹配的条码字符串</param>
    /// <returns>如果条码符合预设规则返回true，否则返回false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Evaluate(BarcodeMatchPreset preset, string parameter, string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return false;

        return preset switch
        {
            BarcodeMatchPreset.StartsWith => barcode.StartsWith(parameter, StringComparison.OrdinalIgnoreCase),
            BarcodeMatchPreset.Contains => barcode.Contains(parameter, StringComparison.OrdinalIgnoreCase),
            BarcodeMatchPreset.NotContains => !barcode.Contains(parameter, StringComparison.OrdinalIgnoreCase),
            BarcodeMatchPreset.AllDigits => Regex.IsMatch(barcode, @"^\d+$"),
            BarcodeMatchPreset.Alphanumeric => Regex.IsMatch(barcode, @"^[A-Za-z0-9]+$"),
            BarcodeMatchPreset.Length => EvaluateLengthRange(parameter, barcode),
            BarcodeMatchPreset.Regex => EvaluateRegex(parameter, barcode),
            _ => false
        };
    }

    /// <summary>
    /// 评估条码正则匹配（使用字符串表达式，保持向后兼容）
    /// </summary>
    /// <param name="expression">匹配表达式字符串，支持多种格式（如"STARTSWITH:prefix"、"CONTAINS:text"等）</param>
    /// <param name="barcode">要匹配的条码字符串</param>
    /// <returns>如果条码符合表达式规则返回true，否则返回false</returns>
    public bool Evaluate(string expression, string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || string.IsNullOrWhiteSpace(expression))
            return false;

        var expr = expression.Trim();

        // 预设选项：以...开头
        if (expr.StartsWith("STARTSWITH:", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = expr.Substring("STARTSWITH:".Length).Trim();
            return Evaluate(BarcodeMatchPreset.StartsWith, prefix, barcode);
        }

        // 预设选项：包含...字符
        if (expr.StartsWith("CONTAINS:", StringComparison.OrdinalIgnoreCase))
        {
            var substring = expr.Substring("CONTAINS:".Length).Trim();
            return Evaluate(BarcodeMatchPreset.Contains, substring, barcode);
        }

        // 预设选项：不包含...字符
        if (expr.StartsWith("NOTCONTAINS:", StringComparison.OrdinalIgnoreCase))
        {
            var substring = expr.Substring("NOTCONTAINS:".Length).Trim();
            return Evaluate(BarcodeMatchPreset.NotContains, substring, barcode);
        }

        // 预设选项：全数字
        if (expr.Equals("ALLDIGITS", StringComparison.OrdinalIgnoreCase))
        {
            return Evaluate(BarcodeMatchPreset.AllDigits, string.Empty, barcode);
        }

        // 预设选项：字母+数字
        if (expr.Equals("ALPHANUMERIC", StringComparison.OrdinalIgnoreCase))
        {
            return Evaluate(BarcodeMatchPreset.Alphanumeric, string.Empty, barcode);
        }

        // 预设选项：指定长度范围 LENGTH:min-max
        if (expr.StartsWith("LENGTH:", StringComparison.OrdinalIgnoreCase))
        {
            var lengthSpec = expr.Substring("LENGTH:".Length).Trim();
            return Evaluate(BarcodeMatchPreset.Length, lengthSpec, barcode);
        }

        // 自定义正则表达式（以REGEX:开头）
        if (expr.StartsWith("REGEX:", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = expr.Substring("REGEX:".Length).Trim();
            return Evaluate(BarcodeMatchPreset.Regex, pattern, barcode);
        }

        // 默认作为正则表达式处理
        return Evaluate(BarcodeMatchPreset.Regex, expr, barcode);
    }

    /// <summary>
    /// 评估长度范围
    /// </summary>
    /// <param name="lengthSpec">长度规格字符串，格式为"min-max"（例如："10-20"）</param>
    /// <param name="barcode">要检查的条码字符串</param>
    /// <returns>如果条码长度在指定范围内返回true，否则返回false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluateLengthRange(string lengthSpec, string barcode)
    {
        var parts = lengthSpec.Split('-');
        if (parts.Length == 2 && 
            int.TryParse(parts[0], out int minLength) && 
            int.TryParse(parts[1], out int maxLength))
        {
            return barcode.Length >= minLength && barcode.Length <= maxLength;
        }
        return false;
    }

    /// <summary>
    /// 评估正则表达式
    /// </summary>
    /// <param name="pattern">正则表达式模式</param>
    /// <param name="barcode">要匹配的条码字符串</param>
    /// <returns>如果条码匹配正则表达式返回true，否则返回false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluateRegex(string pattern, string barcode)
    {
        try
        {
            return Regex.IsMatch(barcode, pattern);
        }
        catch
        {
            return false;
        }
    }
}
