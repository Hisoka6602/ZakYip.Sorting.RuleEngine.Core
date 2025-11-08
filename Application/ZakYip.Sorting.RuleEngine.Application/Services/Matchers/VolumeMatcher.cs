using System.Runtime.CompilerServices;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 体积匹配器 - 支持长宽高和体积的复杂表达式评估
/// Volume matcher - supports complex expressions for length, width, height and volume evaluation
/// </summary>
/// <remarks>
/// This matcher evaluates dimensional expressions for parcel sorting based on DWS (Dimension Weight Scan) data.
/// Supported variables: Length, Width, Height, Volume
/// Supported operators: >, &lt;, >=, &lt;=, ==, &amp;&amp;, ||, and, or
/// 
/// Example expressions:
/// - "Length > 20 and Width > 10"
/// - "Height = 20.5 or Volume > 200"
/// - "Length >= 10 &amp;&amp; Width &lt;= 30 &amp;&amp; Height > 5"
/// </remarks>
public class VolumeMatcher
{
    /// <summary>
    /// 评估体积匹配表达式
    /// Evaluates volume matching expression against DWS data
    /// </summary>
    /// <param name="expression">The expression to evaluate (e.g., "Length > 20 and Width > 10")</param>
    /// <param name="dwsData">The DWS data containing dimensional information</param>
    /// <returns>True if the expression matches the DWS data, false otherwise</returns>
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

    /// <summary>
    /// 标准化逻辑操作符，将 and/or 转换为 &amp;&amp;/||
    /// Normalizes logical operators, converting 'and'/'or' to '&amp;&amp;'/'||'
    /// </summary>
    /// <param name="expression">The expression to normalize</param>
    /// <returns>Normalized expression with standardized operators</returns>
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
    /// 评估布尔表达式，处理逻辑运算符和比较操作
    /// Evaluates boolean expression, handling logical operators and comparisons
    /// </summary>
    /// <param name="expression">The boolean expression to evaluate</param>
    /// <returns>The result of the boolean evaluation</returns>
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

    /// <summary>
    /// 评估单个比较表达式，支持各种比较运算符
    /// Evaluates a single comparison expression, supporting various comparison operators
    /// </summary>
    /// <param name="expression">The comparison expression to evaluate</param>
    /// <returns>The result of the comparison</returns>
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
