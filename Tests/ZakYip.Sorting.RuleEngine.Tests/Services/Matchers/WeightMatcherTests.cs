using ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

namespace ZakYip.Sorting.RuleEngine.Tests.Services.Matchers;

/// <summary>
/// 重量匹配器测试
/// </summary>
public class WeightMatcherTests
{
    private readonly WeightMatcher _matcher = new();

    [Theory]
    [InlineData("Weight > 50", 100, true)]
    [InlineData("Weight > 50", 50, false)]
    [InlineData("Weight > 50", 30, false)]
    public void Evaluate_GreaterThan_ReturnsExpectedResult(string expression, decimal weight, bool expected)
    {
        var result = _matcher.Evaluate(expression, weight);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Weight < 100", 50, true)]
    [InlineData("Weight < 100", 100, false)]
    [InlineData("Weight < 100", 150, false)]
    public void Evaluate_LessThan_ReturnsExpectedResult(string expression, decimal weight, bool expected)
    {
        var result = _matcher.Evaluate(expression, weight);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Weight = 50", 50, true)]
    [InlineData("Weight = 50", 51, false)]
    public void Evaluate_Equals_ReturnsExpectedResult(string expression, decimal weight, bool expected)
    {
        var result = _matcher.Evaluate(expression, weight);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Weight > 10 and Weight < 100", 50, true)]
    [InlineData("Weight > 10 and Weight < 100", 5, false)]
    [InlineData("Weight > 10 and Weight < 100", 150, false)]
    [InlineData("Weight > 10 & Weight < 100", 50, true)]
    public void Evaluate_AndLogic_ReturnsExpectedResult(string expression, decimal weight, bool expected)
    {
        var result = _matcher.Evaluate(expression, weight);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Weight < 10 or Weight > 100", 5, true)]
    [InlineData("Weight < 10 or Weight > 100", 150, true)]
    [InlineData("Weight < 10 or Weight > 100", 50, false)]
    [InlineData("Weight < 10 | Weight > 100", 5, true)]
    public void Evaluate_OrLogic_ReturnsExpectedResult(string expression, decimal weight, bool expected)
    {
        var result = _matcher.Evaluate(expression, weight);
        Assert.Equal(expected, result);
    }
}
