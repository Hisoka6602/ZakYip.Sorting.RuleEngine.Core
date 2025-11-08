using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// OCR匹配器 - 支持基于OCR识别结果的地址段码和电话后缀匹配
/// OCR matcher - supports matching based on OCR recognition results for address segments and phone suffixes
/// </summary>
/// <remarks>
/// This matcher evaluates expressions based on OCR (Optical Character Recognition) data extracted from parcels.
/// It supports matching against various OCR fields including:
/// - Three-segment postal codes (ThreeSegmentCode, FirstSegmentCode, SecondSegmentCode, ThirdSegmentCode)
/// - Recipient and sender addresses
/// - Phone number suffixes
/// 
/// Supports both exact string matching and regular expression pattern matching.
/// 
/// Example expressions:
/// - "firstSegmentCode=^64\d*$" - Match first segment starting with 64
/// - "recipientPhoneSuffix=1234" - Exact match on phone suffix
/// - "recipientAddress=Beijing and senderAddress=Shanghai" - Combined AND condition
/// </remarks>
public class OcrMatcher
{
    /// <summary>
    /// 评估OCR匹配表达式
    /// Evaluates OCR matching expression against OCR data
    /// </summary>
    /// <param name="expression">The expression to evaluate (supports field=value format with and/or operators)</param>
    /// <param name="ocrData">The OCR data containing recognized text information</param>
    /// <returns>True if the expression matches the OCR data, false otherwise</returns>
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
    /// Evaluates a single condition expression
    /// </summary>
    /// <param name="condition">The condition to evaluate (field=value format)</param>
    /// <param name="ocrData">The OCR data to match against</param>
    /// <returns>True if the condition matches, false otherwise</returns>
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
    /// Gets the value of a specified field from OCR data
    /// </summary>
    /// <param name="fieldName">The name of the field to retrieve (case-insensitive)</param>
    /// <param name="ocrData">The OCR data containing the field values</param>
    /// <returns>The field value if found, null otherwise</returns>
    /// <remarks>
    /// Supported field names:
    /// - threesegmentcode: Full three-segment postal code
    /// - firstsegmentcode: First segment of postal code
    /// - secondsegmentcode: Second segment of postal code
    /// - thirdsegmentcode: Third segment of postal code
    /// - recipientaddress: Recipient's address
    /// - senderaddress: Sender's address
    /// - recipientphonesuffix: Last digits of recipient's phone
    /// - senderphonesuffix: Last digits of sender's phone
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
