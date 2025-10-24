using System.Text.Json;
using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 条码正则匹配器
/// </summary>
public class BarcodeRegexMatcher
{
    /// <summary>
    /// 评估条码正则匹配
    /// 支持预设选项和自定义正则
    /// </summary>
    public bool Evaluate(string expression, string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || string.IsNullOrWhiteSpace(expression))
            return false;

        var expr = expression.Trim();

        // 预设选项：以...开头
        if (expr.StartsWith("STARTSWITH:", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = expr.Substring("STARTSWITH:".Length).Trim();
            return barcode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // 预设选项：包含...字符
        if (expr.StartsWith("CONTAINS:", StringComparison.OrdinalIgnoreCase))
        {
            var substring = expr.Substring("CONTAINS:".Length).Trim();
            return barcode.Contains(substring, StringComparison.OrdinalIgnoreCase);
        }

        // 预设选项：不包含...字符
        if (expr.StartsWith("NOTCONTAINS:", StringComparison.OrdinalIgnoreCase))
        {
            var substring = expr.Substring("NOTCONTAINS:".Length).Trim();
            return !barcode.Contains(substring, StringComparison.OrdinalIgnoreCase);
        }

        // 预设选项：全数字
        if (expr.Equals("ALLDIGITS", StringComparison.OrdinalIgnoreCase))
        {
            return Regex.IsMatch(barcode, @"^\d+$");
        }

        // 预设选项：字母+数字
        if (expr.Equals("ALPHANUMERIC", StringComparison.OrdinalIgnoreCase))
        {
            return Regex.IsMatch(barcode, @"^[A-Za-z0-9]+$");
        }

        // 预设选项：指定长度范围 LENGTH:min-max
        if (expr.StartsWith("LENGTH:", StringComparison.OrdinalIgnoreCase))
        {
            var lengthSpec = expr.Substring("LENGTH:".Length).Trim();
            var parts = lengthSpec.Split('-');
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out int minLength) && 
                int.TryParse(parts[1], out int maxLength))
            {
                return barcode.Length >= minLength && barcode.Length <= maxLength;
            }
        }

        // 自定义正则表达式（以REGEX:开头）
        if (expr.StartsWith("REGEX:", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = expr.Substring("REGEX:".Length).Trim();
            try
            {
                return Regex.IsMatch(barcode, pattern);
            }
            catch
            {
                return false;
            }
        }

        // 默认作为正则表达式处理
        try
        {
            return Regex.IsMatch(barcode, expr);
        }
        catch
        {
            return false;
        }
    }
}
