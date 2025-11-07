using System.Runtime.CompilerServices;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using System.Text.RegularExpressions;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 低代码表达式匹配器
/// 支持用户自定义表达式，可混合使用各种条件
/// 例如：if(Weight>10) and firstSegmentCode=^64\d*$
/// </summary>
public class LowCodeExpressionMatcher
{
    private readonly BarcodeRegexMatcher _barcodeMatcher = new();
    private readonly WeightMatcher _weightMatcher = new();
    private readonly VolumeMatcher _volumeMatcher = new();
    private readonly OcrMatcher _ocrMatcher = new();

    /// <summary>
    /// 评估低代码表达式
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Evaluate(
        string expression,
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        WcsApiResponse? thirdPartyResponse)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        try
        {
            var expr = expression.Trim();

            // 移除if()包装
            if (expr.StartsWith("if(", StringComparison.OrdinalIgnoreCase) && expr.EndsWith(")"))
            {
                expr = expr.Substring(3, expr.Length - 4).Trim();
            }

            // 处理AND逻辑
            if (expr.Contains(" and ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = expr.Split(new[] { " and " }, StringSplitOptions.None);
                return parts.All(p => EvaluateCondition(p.Trim(), parcelInfo, dwsData, thirdPartyResponse));
            }

            // 处理OR逻辑
            if (expr.Contains(" or ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = expr.Split(new[] { " or " }, StringSplitOptions.None);
                return parts.Any(p => EvaluateCondition(p.Trim(), parcelInfo, dwsData, thirdPartyResponse));
            }

            // 单个条件
            return EvaluateCondition(expr, parcelInfo, dwsData, thirdPartyResponse);
        }
        catch
        {
            return false;
        }
    }

    private bool EvaluateCondition(
        string condition,
        ParcelInfo parcelInfo,
        DwsData? dwsData,
        WcsApiResponse? thirdPartyResponse)
    {
        condition = condition.Trim();

        // Weight条件
        if (condition.Contains("Weight", StringComparison.OrdinalIgnoreCase))
        {
            if (dwsData != null)
            {
                return _weightMatcher.Evaluate(condition, dwsData.Weight);
            }
            return false;
        }

        // Volume, Length, Width, Height条件
        if (condition.Contains("Volume", StringComparison.OrdinalIgnoreCase) ||
            condition.Contains("Length", StringComparison.OrdinalIgnoreCase) ||
            condition.Contains("Width", StringComparison.OrdinalIgnoreCase) ||
            condition.Contains("Height", StringComparison.OrdinalIgnoreCase))
        {
            if (dwsData != null)
            {
                return _volumeMatcher.Evaluate(condition, dwsData);
            }
            return false;
        }

        // OCR字段条件
        if (IsOcrField(condition))
        {
            if (thirdPartyResponse?.OcrData != null)
            {
                return _ocrMatcher.Evaluate(condition, thirdPartyResponse.OcrData);
            }
            return false;
        }

        // Barcode条件
        if (condition.Contains("Barcode", StringComparison.OrdinalIgnoreCase))
        {
            var barcode = parcelInfo.Barcode ?? dwsData?.Barcode ?? string.Empty;
            // 提取条码匹配表达式
            var match = Regex.Match(condition, @"Barcode\s*=\s*(.+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var pattern = match.Groups[1].Value.Trim();
                return _barcodeMatcher.Evaluate(pattern, barcode);
            }
        }

        return false;
    }

    private bool IsOcrField(string condition)
    {
        var ocrFields = new[]
        {
            "threesegmentcode", "firstsegmentcode", "secondsegmentcode", "thirdsegmentcode",
            "recipientaddress", "senderaddress", "recipientphonesuffix", "senderphonesuffix"
        };

        return ocrFields.Any(field => condition.Contains(field, StringComparison.OrdinalIgnoreCase));
    }
}
