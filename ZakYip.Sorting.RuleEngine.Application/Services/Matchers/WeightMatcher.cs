using System.Data;

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
    public bool Evaluate(string expression, decimal weight)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        try
        {
            // 替换表达式中的Weight为实际值
            var expr = expression
                .Replace("Weight", weight.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("and", "&&", StringComparison.OrdinalIgnoreCase)
                .Replace("or", "||", StringComparison.OrdinalIgnoreCase)
                .Replace("&", "&&")
                .Replace("|", "||")
                .Replace("=", "==");

            // 修正双等号重复问题
            expr = expr.Replace("====", "==");

            // 计算布尔表达式
            return EvaluateBooleanExpression(expr);
        }
        catch
        {
            return false;
        }
    }

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
        else if (expression.Contains(">"))
        {
            var parts = expression.Split(new[] { ">" }, StringSplitOptions.None);
            if (parts.Length == 2 && 
                decimal.TryParse(parts[0].Trim(), out decimal left) && 
                decimal.TryParse(parts[1].Trim(), out decimal right))
            {
                return left > right;
            }
        }
        else if (expression.Contains("<"))
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
