using System.Runtime.CompilerServices;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 体积匹配器 - 支持长宽高和体积的复杂表达式评估
/// Volume Matcher - Supports complex expression evaluation for length, width, height, and volume
/// </summary>
/// <remarks>
/// 此匹配器基于DWS（尺寸重量扫描）数据评估包裹分拣的尺寸表达式。
/// This matcher evaluates dimension expressions for parcel sorting based on DWS data.
/// 支持的变量：Length（长）、Width（宽）、Height（高）、Volume（体积）
/// Supported variables: Length, Width, Height, Volume
/// 支持的运算符：>、&lt;、>=、&lt;=、==、&amp;&amp;、||、and、or
/// Supported operators: >, &lt;, >=, &lt;=, ==, &amp;&amp;, ||, and, or
/// 
/// 表达式示例 / Expression examples:
/// - "Length > 20 and Width > 10" - 长度大于20且宽度大于10
/// - "Height = 20.5 or Volume > 200" - 高度等于20.5或体积大于200
/// - "Length >= 10 &amp;&amp; Width &lt;= 30 &amp;&amp; Height > 5" - 复合条件
/// </remarks>
public class VolumeMatcher
{
    /// <summary>
    /// 评估体积匹配表达式
    /// Evaluate volume matching expression
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
