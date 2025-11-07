using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// OCR匹配器
/// 支持地址段码和电话后缀匹配
/// 例如：firstSegmentCode=^64\d*$, recipientPhoneSuffix=1234
/// </summary>
public class OcrMatcher
{
    /// <summary>
    /// 评估OCR匹配表达式
    /// </summary>
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
