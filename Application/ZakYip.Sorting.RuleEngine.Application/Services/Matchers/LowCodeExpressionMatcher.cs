using System.Runtime.CompilerServices;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using System.Text.RegularExpressions;

namespace ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

/// <summary>
/// 低代码表达式匹配器
/// 支持用户自定义表达式，可混合使用各种条件
/// 支持表达式格式：if(条件) and/or 其他条件
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
    /// <param name="expression">低代码表达式字符串，可包含if()包装和逻辑运算符</param>
    /// <param name="parcelInfo">包裹基本信息</param>
    /// <param name="dwsData">DWS（尺寸重量扫描）数据，可选</param>
    /// <param name="thirdPartyResponse">第三方API响应数据，可选</param>
    /// <returns>如果表达式评估为真返回true，否则返回false</returns>
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

    /// <summary>
    /// 评估单个条件表达式
    /// 根据条件类型（重量、体积、OCR字段、条码等）选择相应的匹配器进行评估
    /// </summary>
    /// <param name="condition">要评估的单个条件</param>
    /// <param name="parcelInfo">包裹基本信息</param>
    /// <param name="dwsData">DWS数据，可选</param>
    /// <param name="thirdPartyResponse">第三方API响应数据，可选</param>
    /// <returns>如果条件匹配返回true，否则返回false</returns>
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

    /// <summary>
    /// 判断条件是否为OCR字段
    /// 检查条件中是否包含OCR相关的字段名称
    /// </summary>
    /// <param name="condition">要检查的条件字符串</param>
    /// <returns>如果条件包含OCR字段返回true，否则返回false</returns>
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
