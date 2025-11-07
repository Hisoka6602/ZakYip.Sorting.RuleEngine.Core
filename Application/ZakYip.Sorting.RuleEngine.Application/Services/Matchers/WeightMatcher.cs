using System.Data;
using System.Runtime.CompilerServices;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 重量匹配器
/// 支持表达式：Weight > 50, Weight &lt; 100 and Weight > 10, etc.
/// </summary>
public class WeightMatcher
{
    /// <summary>
    /// 评估重量匹配表达式
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Evaluate(string expression, decimal weight)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        try
        {
            // 替换表达式中的Weight为实际值
            var expr = expression
                .Replace("Weight", weight.ToString(), StringComparison.OrdinalIgnoreCase);

            // 标准化逻辑操作符
            expr = NormalizeLogicalOperators(expr);

            // 计算布尔表达式
            return EvaluateBooleanExpression(expr);
        }
        catch
        {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string NormalizeLogicalOperators(string expression)
    {
        // 替换and/or为&&/||
        expression = System.Text.RegularExpressions.Regex.Replace(
            expression, 
            @"\band\b", 
            "&&", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
        expression = System.Text.RegularExpressions.Regex.Replace(
            expression, 
            @"\bor\b", 
            "||", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // 替换单个&或|为双符号
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"(?<![&|])&(?![&|])", "&&");
        expression = System.Text.RegularExpressions.Regex.Replace(expression, @"(?<![&|])\|(?![&|])", "||");

        return expression;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluateBooleanExpression(string expression)
    {
        try
        {
            // 简单的表达式评估器
            // 支持 &&, ||, >, <, >=, <=, ==
            
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EvaluateComparison(string expression)
    {
        expression = expression.Trim();

        if (expression.Contains(">="))
        {
            var parts = expression.Split(new[] { ">=" }, StringSplitOptions.None);
            if (parts.Length == 2 && 
                decimal.TryParse(parts[0].Trim(), out decimal left) && 
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left >= right;
            }
        }
        else if (expression.Contains("<="))
        {
            var parts = expression.Split(new[] { "<=" }, StringSplitOptions.None);
            if (parts.Length == 2 && 
                decimal.TryParse(parts[0].Trim(), out decimal left) && 
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left <= right;
            }
        }
        else if (expression.Contains("=="))
        {
            var parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
            if (parts.Length == 2 && 
                decimal.TryParse(parts[0].Trim(), out decimal left) && 
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left == right;
            }
        }
        else if (expression.Contains("=") && !expression.Contains(">") && !expression.Contains("<"))
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
        else if (expression.Contains(">") && !expression.Contains(">="))
        {
            var parts = expression.Split(new[] { ">" }, StringSplitOptions.None);
            if (parts.Length == 2 && 
                decimal.TryParse(parts[0].Trim(), out decimal left) && 
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left > right;
            }
        }
        else if (expression.Contains("<") && !expression.Contains("<="))
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
