using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Tests.Helpers;

/// <summary>
/// 测试数据构建器
/// Test data builder for creating test fixtures
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// 创建测试用的排序规则
    /// Create a test sorting rule
    /// </summary>
    public static SortingRule CreateSortingRule(
        string ruleId = "TEST-R001",
        string ruleName = "Test Rule",
        string conditionExpression = "Weight > 1000",
        string targetChute = "CHUTE-A01",
        int priority = 1,
        MatchingMethodType matchingMethod = MatchingMethodType.WeightMatch,
        bool isEnabled = true)
    {
        return new SortingRule
        {
            RuleId = ruleId,
            RuleName = ruleName,
            ConditionExpression = conditionExpression,
            TargetChute = targetChute,
            Priority = priority,
            MatchingMethod = matchingMethod,
            IsEnabled = isEnabled,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建测试用的DWS数据
    /// Create test DWS data
    /// </summary>
    public static DwsData CreateDwsData(
        string barcode = "TEST123456",
        decimal weight = 1000,
        decimal length = 300,
        decimal width = 200,
        decimal height = 150,
        decimal? volume = null)
    {
        return new DwsData
        {
            Barcode = barcode,
            Weight = weight,
            Length = length,
            Width = width,
            Height = height,
            Volume = volume ?? (length * width * height / 1000)
        };
    }

    /// <summary>
    /// 创建测试用的包裹信息
    /// Create test parcel info
    /// </summary>
    public static ParcelInfo CreateParcelInfo(
        string parcelId = "PKG001",
        string cartNumber = "CART001",
        string? barcode = null,
        ParcelStatus status = ParcelStatus.Pending)
    {
        return new ParcelInfo
        {
            ParcelId = parcelId,
            CartNumber = cartNumber,
            Barcode = barcode,
            Status = status,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建测试用的OCR数据
    /// Create test OCR data
    /// </summary>
    public static OcrData CreateOcrData(
        string? threeSegmentCode = null,
        string? firstSegmentCode = "64",
        string? secondSegmentCode = "12",
        string? thirdSegmentCode = "34",
        string? recipientAddress = null,
        string? senderAddress = null)
    {
        return new OcrData
        {
            ThreeSegmentCode = threeSegmentCode ?? $"{firstSegmentCode}{secondSegmentCode}{thirdSegmentCode}",
            FirstSegmentCode = firstSegmentCode,
            SecondSegmentCode = secondSegmentCode,
            ThirdSegmentCode = thirdSegmentCode,
            RecipientAddress = recipientAddress ?? "Test Recipient Address",
            SenderAddress = senderAddress ?? "Test Sender Address",
            RecipientPhoneSuffix = "1234",
            SenderPhoneSuffix = "5678"
        };
    }

    /// <summary>
    /// 创建测试用的WCS API响应
    /// Create test wcs API response
    /// </summary>
    public static WcsApiResponse CreateThirdPartyResponse(
        bool success = true,
        string code = "200",
        string message = "Success",
        string? data = null,
        OcrData? ocrData = null)
    {
        return new WcsApiResponse
        {
            Success = success,
            Code = code,
            Message = message,
            Data = data ?? "Test Data",
            OcrData = ocrData
        };
    }

    /// <summary>
    /// 创建一批测试规则
    /// Create a batch of test rules
    /// </summary>
    public static List<SortingRule> CreateMultipleRules(int count = 3)
    {
        var rules = new List<SortingRule>();
        for (int i = 1; i <= count; i++)
        {
            rules.Add(CreateSortingRule(
                ruleId: $"R{i:D3}",
                ruleName: $"Rule {i}",
                conditionExpression: $"Weight > {i * 1000}",
                targetChute: $"CHUTE-{(char)('A' + (i - 1) % 26)}{i:D2}",
                priority: i
            ));
        }
        return rules;
    }

    /// <summary>
    /// 创建一批测试包裹
    /// Create a batch of test parcels
    /// </summary>
    public static List<ParcelInfo> CreateMultipleParcels(int count = 5)
    {
        var parcels = new List<ParcelInfo>();
        for (int i = 1; i <= count; i++)
        {
            parcels.Add(CreateParcelInfo(
                parcelId: $"PKG{i:D3}",
                cartNumber: $"CART{i:D3}",
                barcode: $"BC{i:D8}"
            ));
        }
        return parcels;
    }
}
