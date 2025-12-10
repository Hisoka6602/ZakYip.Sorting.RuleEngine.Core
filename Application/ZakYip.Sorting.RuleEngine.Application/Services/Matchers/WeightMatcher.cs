using System.Runtime.CompilerServices;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 重量匹配器 - 支持重量表达式评估
/// Weight Matcher - Supports weight expression evaluation
/// </summary>
/// <remarks>
/// 支持表达式：Weight > 50, Weight &lt; 100 and Weight > 10, etc.
/// Supported expressions: Weight > 50, Weight &lt; 100 and Weight > 10, etc.
/// </remarks>
public class WeightMatcher
{
    /// <summary>
    /// 评估重量匹配表达式
    /// Evaluate weight matching expression
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

            // 使用共享的表达式评估器
            expr = BaseExpressionEvaluator.NormalizeLogicalOperators(expr);
            return BaseExpressionEvaluator.EvaluateBooleanExpression(expr);
        }
        catch
        {
            return false;
        }
    }
}
