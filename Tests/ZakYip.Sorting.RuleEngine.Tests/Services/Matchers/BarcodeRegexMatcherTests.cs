using ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

namespace ZakYip.Sorting.RuleEngine.Tests.Services.Matchers;

/// <summary>
/// 条码正则匹配器测试
/// </summary>
public class BarcodeRegexMatcherTests
{
    private readonly BarcodeRegexMatcher _matcher = new();

    [Theory]
    [InlineData("STARTSWITH:SF", "SF123456", true)]
    [InlineData("STARTSWITH:SF", "123456SF", false)]
    [InlineData("STARTSWITH:123", "123456", true)]
    public void Evaluate_StartsWithPreset_ReturnsExpectedResult(string expression, string barcode, bool expected)
    {
        var result = _matcher.Evaluate(expression, barcode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("CONTAINS:ABC", "123ABC456", true)]
    [InlineData("CONTAINS:ABC", "123456", false)]
    [InlineData("CONTAINS:SF", "SFDHL123", true)]
    public void Evaluate_ContainsPreset_ReturnsExpectedResult(string expression, string barcode, bool expected)
    {
        var result = _matcher.Evaluate(expression, barcode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("NOTCONTAINS:ABC", "123456", true)]
    [InlineData("NOTCONTAINS:ABC", "123ABC456", false)]
    public void Evaluate_NotContainsPreset_ReturnsExpectedResult(string expression, string barcode, bool expected)
    {
        var result = _matcher.Evaluate(expression, barcode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ALLDIGITS", "123456", true)]
    [InlineData("ALLDIGITS", "12A456", false)]
    [InlineData("ALLDIGITS", "0987654321", true)]
    public void Evaluate_AllDigitsPreset_ReturnsExpectedResult(string expression, string barcode, bool expected)
    {
        var result = _matcher.Evaluate(expression, barcode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ALPHANUMERIC", "ABC123", true)]
    [InlineData("ALPHANUMERIC", "ABC-123", false)]
    [InlineData("ALPHANUMERIC", "123456", true)]
    public void Evaluate_AlphanumericPreset_ReturnsExpectedResult(string expression, string barcode, bool expected)
    {
        var result = _matcher.Evaluate(expression, barcode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("LENGTH:5-10", "123456", true)]
    [InlineData("LENGTH:5-10", "1234", false)]
    [InlineData("LENGTH:5-10", "12345678901", false)]
    [InlineData("LENGTH:5-10", "12345", true)]
    public void Evaluate_LengthRangePreset_ReturnsExpectedResult(string expression, string barcode, bool expected)
    {
        var result = _matcher.Evaluate(expression, barcode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("REGEX:^SF\\d{6}$", "SF123456", true)]
    [InlineData("REGEX:^SF\\d{6}$", "SF12345", false)]
    [InlineData("REGEX:^[A-Z]{2}\\d+$", "AB123", true)]
    public void Evaluate_CustomRegex_ReturnsExpectedResult(string expression, string barcode, bool expected)
    {
        var result = _matcher.Evaluate(expression, barcode);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("^64\\d*$", "641234", true)]
    [InlineData("^64\\d*$", "741234", false)]
    public void Evaluate_DirectRegex_ReturnsExpectedResult(string expression, string barcode, bool expected)
    {
        var result = _matcher.Evaluate(expression, barcode);
        Assert.Equal(expected, result);
    }
}
