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
    /// <param name="expression">重量匹配表达式，支持比较运算符和逻辑运算符（如"Weight &gt; 50 and Weight &lt; 100"）</param>
    /// <param name="weight">包裹的实际重量值（单位：千克）</param>
    /// <returns>如果重量符合表达式条件返回true，否则返回false</returns>
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

    /// <summary>
    /// 标准化逻辑操作符，将 and/or 转换为 &amp;&amp;/||
    /// 将用户友好的逻辑运算符转换为程序可识别的标准格式
    /// </summary>
    /// <param name="expression">原始表达式，可能包含 and、or、单个 &amp; 或 |</param>
    /// <returns>标准化后的表达式，使用 &amp;&amp; 和 || 表示逻辑运算</returns>
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

    /// <summary>
    /// 评估布尔表达式，支持 &amp;&amp;, ||, &gt;, &lt;, &gt;=, &lt;=, == 操作符
    /// 递归处理逻辑运算符，并评估各个比较条件
    /// </summary>
    /// <param name="expression">布尔表达式，可能包含多个由逻辑运算符连接的比较条件</param>
    /// <returns>表达式的布尔计算结果</returns>
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

    /// <summary>
    /// 评估比较表达式，解析并计算数值比较
    /// 支持六种比较运算符：&gt;=, &lt;=, ==, =, &gt;, &lt;
    /// </summary>
    /// <param name="expression">单个比较表达式（如 50 &gt; 30）</param>
    /// <returns>比较的布尔结果</returns>
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
