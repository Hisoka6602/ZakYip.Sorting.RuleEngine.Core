using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// OCR匹配器 - 支持基于OCR识别结果的地址段码和电话后缀匹配
/// </summary>
/// <remarks>
/// 此匹配器基于OCR（光学字符识别）数据评估包裹的文字识别结果。
/// 支持匹配以下OCR字段：
/// - 三段码相关：ThreeSegmentCode（三段码）、FirstSegmentCode（第一段）、SecondSegmentCode（第二段）、ThirdSegmentCode（第三段）
/// - 地址信息：RecipientAddress（收件人地址）、SenderAddress（寄件人地址）
/// - 电话后缀：RecipientPhoneSuffix（收件人电话后缀）、SenderPhoneSuffix（寄件人电话后缀）
/// 
/// 支持精确字符串匹配和正则表达式模式匹配。
/// 
/// 表达式示例：
/// - "firstSegmentCode=^64\d*$" - 匹配以64开头的第一段码
/// - "recipientPhoneSuffix=1234" - 精确匹配电话后缀
/// - "recipientAddress=北京 and senderAddress=上海" - 收件地址包含北京且寄件地址包含上海
/// </remarks>
public class OcrMatcher
{
    /// <summary>
    /// 评估OCR匹配表达式
    /// </summary>
    /// <param name="expression">匹配表达式，支持field=value格式，可使用and/or运算符</param>
    /// <param name="ocrData">包含识别文字信息的OCR数据</param>
    /// <returns>如果表达式与OCR数据匹配返回true，否则返回false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Evaluate(string expression, OcrData? ocrData)
    {
        if (string.IsNullOrWhiteSpace(expression) || ocrData == null)
            return false;

        try
        {
            // 处理AND逻辑
            if (expression.Contains(" and ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = expression.Split(new[] { " and " }, StringSplitOptions.None);
                return parts.All(p => EvaluateSingleCondition(p.Trim(), ocrData));
            }

            // 处理OR逻辑
            if (expression.Contains(" or ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = expression.Split(new[] { " or " }, StringSplitOptions.None);
                return parts.Any(p => EvaluateSingleCondition(p.Trim(), ocrData));
            }

            // 单个条件
            return EvaluateSingleCondition(expression, ocrData);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 评估单个条件表达式
    /// </summary>
    /// <param name="condition">要评估的条件，格式为field=value</param>
    /// <param name="ocrData">要匹配的OCR数据</param>
    /// <returns>如果条件匹配返回true，否则返回false</returns>
    private bool EvaluateSingleCondition(string condition, OcrData ocrData)
    {
        condition = condition.Trim();

        // 提取字段名和值
        var match = Regex.Match(condition, @"(\w+)\s*=\s*(.+)");
        if (!match.Success)
            return false;

        var fieldName = match.Groups[1].Value.Trim();
        var expectedValue = match.Groups[2].Value.Trim();

        // 获取实际字段值
        var actualValue = GetFieldValue(fieldName, ocrData);
        if (actualValue == null)
            return false;

        // 如果期望值是正则表达式（以^开头或包含正则特殊字符）
        if (expectedValue.StartsWith("^") || expectedValue.Contains("\\d") || expectedValue.Contains("*") || expectedValue.Contains("+"))
        {
            try
            {
                return Regex.IsMatch(actualValue, expectedValue);
            }
            catch
            {
                return false;
            }
        }

        // 否则进行字符串相等比较
        return actualValue.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取OCR数据中指定字段的值
    /// </summary>
    /// <param name="fieldName">要获取的字段名称（不区分大小写）</param>
    /// <param name="ocrData">包含字段值的OCR数据</param>
    /// <returns>找到字段时返回字段值，否则返回null</returns>
    /// <remarks>
    /// 支持的字段名称：
    /// - threesegmentcode: 完整的三段码
    /// - firstsegmentcode: 三段码的第一段
    /// - secondsegmentcode: 三段码的第二段
    /// - thirdsegmentcode: 三段码的第三段
    /// - recipientaddress: 收件人地址
    /// - senderaddress: 寄件人地址
    /// - recipientphonesuffix: 收件人电话后缀
    /// - senderphonesuffix: 寄件人电话后缀
    /// </remarks>
    private string? GetFieldValue(string fieldName, OcrData ocrData)
    {
        return fieldName.ToLower() switch
        {
            "threesegmentcode" => ocrData.ThreeSegmentCode,
            "firstsegmentcode" => ocrData.FirstSegmentCode,
            "secondsegmentcode" => ocrData.SecondSegmentCode,
            "thirdsegmentcode" => ocrData.ThirdSegmentCode,
            "recipientaddress" => ocrData.RecipientAddress,
            "senderaddress" => ocrData.SenderAddress,
            "recipientphonesuffix" => ocrData.RecipientPhoneSuffix,
            "senderphonesuffix" => ocrData.SenderPhoneSuffix,
            _ => null
        };
    }
}
