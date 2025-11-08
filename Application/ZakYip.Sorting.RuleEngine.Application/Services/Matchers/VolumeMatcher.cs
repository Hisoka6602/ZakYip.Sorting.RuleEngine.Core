using System.Runtime.CompilerServices;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 体积匹配器 - 支持长宽高和体积的复杂表达式评估
/// </summary>
/// <remarks>
/// 此匹配器基于DWS（尺寸重量扫描）数据评估包裹分拣的尺寸表达式。
/// 支持的变量：Length（长）、Width（宽）、Height（高）、Volume（体积）
/// 支持的运算符：>、&lt;、>=、&lt;=、==、&amp;&amp;、||、and、or
/// 
/// 表达式示例：
/// - "Length > 20 and Width > 10"
/// - "Height = 20.5 or Volume > 200"
/// - "Length >= 10 &amp;&amp; Width &lt;= 30 &amp;&amp; Height > 5"
/// </remarks>
public class VolumeMatcher
{
    /// <summary>
    /// 评估体积匹配表达式
    /// </summary>
    /// <param name="expression">要评估的表达式（例如："Length > 20 and Width > 10"）</param>
    /// <param name="dwsData">包含尺寸信息的DWS数据</param>
    /// <returns>如果表达式与DWS数据匹配则返回true，否则返回false</returns>
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
