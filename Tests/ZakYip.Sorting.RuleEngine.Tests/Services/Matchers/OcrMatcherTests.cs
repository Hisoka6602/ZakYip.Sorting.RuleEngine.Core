using ZakYip.Sorting.RuleEngine.Application.Services.Matchers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Tests.Services.Matchers;

/// <summary>
/// OCR匹配器测试
/// </summary>
public class OcrMatcherTests
{
    private readonly OcrMatcher _matcher = new();

    [Fact]
    public void Evaluate_FirstSegmentCodeWithRegex_ReturnsExpectedResult()
    {
        var ocrData = new OcrData
        {
            FirstSegmentCode = "641234",
            SecondSegmentCode = "5678",
            ThirdSegmentCode = "90"
        };

        var result = _matcher.Evaluate("firstSegmentCode=^64\\d*$", ocrData);
        Assert.True(result);

        result = _matcher.Evaluate("firstSegmentCode=^74\\d*$", ocrData);
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_RecipientPhoneSuffix_ReturnsExpectedResult()
    {
        var ocrData = new OcrData
        {
            RecipientPhoneSuffix = "1234",
            SenderPhoneSuffix = "5678"
        };

        var result = _matcher.Evaluate("recipientPhoneSuffix=1234", ocrData);
        Assert.True(result);

        result = _matcher.Evaluate("recipientPhoneSuffix=5678", ocrData);
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_AndLogic_ReturnsExpectedResult()
    {
        var ocrData = new OcrData
        {
            FirstSegmentCode = "641234",
            RecipientPhoneSuffix = "1234"
        };

        var result = _matcher.Evaluate("firstSegmentCode=^64\\d*$ and recipientPhoneSuffix=1234", ocrData);
        Assert.True(result);

        result = _matcher.Evaluate("firstSegmentCode=^74\\d*$ and recipientPhoneSuffix=1234", ocrData);
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_OrLogic_ReturnsExpectedResult()
    {
        var ocrData = new OcrData
        {
            FirstSegmentCode = "641234",
            RecipientPhoneSuffix = "5678"
        };

        var result = _matcher.Evaluate("firstSegmentCode=^64\\d*$ or recipientPhoneSuffix=1234", ocrData);
        Assert.True(result);

        result = _matcher.Evaluate("firstSegmentCode=^74\\d*$ or recipientPhoneSuffix=9999", ocrData);
        Assert.False(result);
    }
}
