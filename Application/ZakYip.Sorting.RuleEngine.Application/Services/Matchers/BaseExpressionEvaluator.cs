using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 基础表达式评估器 - 提供通用的布尔表达式评估逻辑
/// Base Expression Evaluator - Provides common boolean expression evaluation logic
/// </summary>
/// <remarks>
/// 此类提取了 VolumeMatcher 和 WeightMatcher 中的共享逻辑，避免代码重复。
/// This class extracts shared logic from VolumeMatcher and WeightMatcher to avoid code duplication.
/// 支持的运算符：>, <, >=, <=, ==, =, &&, ||, and, or
/// Supported operators: >, <, >=, <=, ==, =, &&, ||, and, or
/// </remarks>
internal static class BaseExpressionEvaluator
{
    /// <summary>
    /// 标准化逻辑操作符，将 and/or 转换为 &amp;&amp;/||
    /// Normalize logical operators, convert and/or to &amp;&amp;/||
    /// </summary>
    /// <param name="expression">原始表达式，可能包含 and、or、单个 &amp; 或 |</param>
    /// <returns>标准化后的表达式，使用 &amp;&amp; 和 || 表示逻辑运算</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NormalizeLogicalOperators(string expression)
    {
        // 替换and/or为&&/||
        expression = Regex.Replace(
            expression, 
            @"\band\b", 
            "&&", 
            RegexOptions.IgnoreCase);
            
        expression = Regex.Replace(
            expression, 
            @"\bor\b", 
            "||", 
            RegexOptions.IgnoreCase);

        // 替换单个&或|为双符号
        expression = Regex.Replace(expression, @"(?<![&|])&(?![&|])", "&&");
        expression = Regex.Replace(expression, @"(?<![&|])\|(?![&|])", "||");

        return expression;
    }

    /// <summary>
    /// 评估布尔表达式，支持 &amp;&amp;, ||, &gt;, &lt;, &gt;=, &lt;=, == 操作符
    /// Evaluate boolean expression with support for &&, ||, >, <, >=, <=, == operators
    /// </summary>
    /// <param name="expression">布尔表达式，可能包含多个由逻辑运算符连接的比较条件</param>
    /// <returns>表达式的布尔计算结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EvaluateBooleanExpression(string expression)
    {
        try
        {
            // 处理OR逻辑
            if (expression.Contains("||"))
            {
                var parts = expression.Split(new[] { "||" }, StringSplitOptions.None);
                return parts.Any(p => EvaluateBooleanExpression(p.Trim()));
            }

            // 处理AND逻辑
            if (expression.Contains("&&"))
            {
                var parts = expression.Split(new[] { "&&" }, StringSplitOptions.None);
                return parts.All(p => EvaluateBooleanExpression(p.Trim()));
            }

            // 处理单个比较表达式
            return EvaluateComparison(expression);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 评估单个比较表达式，支持各种比较运算符
    /// Evaluate single comparison expression with various comparison operators
    /// 支持六种比较运算符：&gt;=, &lt;=, ==, =, &gt;, &lt;
    /// </summary>
    /// <param name="expression">单个比较表达式（如 50 &gt; 30）</param>
    /// <returns>比较的布尔结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EvaluateComparison(string expression)
    {
        expression = expression.Trim();

        if (expression.Contains(">=", StringComparison.Ordinal))
        {
            var parts = expression.Split(new[] { ">=" }, StringSplitOptions.None);
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0].Trim(), out decimal left) &&
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left >= right;
            }
        }
        else if (expression.Contains("<=", StringComparison.Ordinal))
        {
            var parts = expression.Split(new[] { "<=" }, StringSplitOptions.None);
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0].Trim(), out decimal left) &&
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left <= right;
            }
        }
        else if (expression.Contains("==", StringComparison.Ordinal))
        {
            var parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0].Trim(), out decimal left) &&
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left == right;
            }
        }
        else if (expression.Contains('=') && !expression.Contains('>') && !expression.Contains('<'))
        {
            // 处理单个等号（不是>=或<=）
            var parts = expression.Split(new[] { "=" }, StringSplitOptions.None);
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0].Trim(), out decimal left) &&
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left == right;
            }
        }
        else if (expression.Contains('>') && !expression.Contains(">=", StringComparison.Ordinal))
        {
            var parts = expression.Split(new[] { ">" }, StringSplitOptions.None);
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0].Trim(), out decimal left) &&
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left > right;
            }
        }
        else if (expression.Contains('<') && !expression.Contains("<=", StringComparison.Ordinal))
        {
            var parts = expression.Split(new[] { "<" }, StringSplitOptions.None);
            if (parts.Length == 2 &&
                decimal.TryParse(parts[0].Trim(), out decimal left) &&
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left < right;
            }
        }

        return false;
    }
}
