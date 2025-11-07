using System.Runtime.CompilerServices;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 体积匹配器
/// 支持长宽高和体积的复杂表达式
/// 例如：Length > 20 and Width > 10 or Height = 20.5 or Volume > 200
/// </summary>
public class VolumeMatcher
{
    /// <summary>
    /// 评估体积匹配表达式
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Evaluate(string expression, DwsData dwsData)
    {
        if (string.IsNullOrWhiteSpace(expression) || dwsData == null)
            return false;

        try
        {
            // 替换表达式中的变量为实际值
            var expr = expression
                .Replace("Length", dwsData.Length.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("Width", dwsData.Width.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("Height", dwsData.Height.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("Volume", dwsData.Volume.ToString(), StringComparison.OrdinalIgnoreCase);

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

    private bool EvaluateBooleanExpression(string expression)
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
