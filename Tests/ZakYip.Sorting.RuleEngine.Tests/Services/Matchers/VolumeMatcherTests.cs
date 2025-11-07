using ZakYip.Sorting.RuleEngine.Application.Services.Matchers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Tests.Services.Matchers;

/// <summary>
/// 体积匹配器测试
/// </summary>
public class VolumeMatcherTests
{
    private readonly VolumeMatcher _matcher = new();

    [Fact]
    public void Evaluate_LengthCondition_ReturnsExpectedResult()
    {
        var dwsData = new DwsData
        {
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var result = _matcher.Evaluate("Length > 250", dwsData);
        Assert.True(result);

        result = _matcher.Evaluate("Length < 250", dwsData);
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_VolumeCondition_ReturnsExpectedResult()
    {
        var dwsData = new DwsData
        {
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var result = _matcher.Evaluate("Volume > 5000", dwsData);
        Assert.True(result);

        result = _matcher.Evaluate("Volume < 5000", dwsData);
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_ComplexExpression_ReturnsExpectedResult()
    {
        var dwsData = new DwsData
        {
            Length = 300,
            Width = 200,
            Height = 150,
            Volume = 9000
        };

        var result = _matcher.Evaluate("Length > 200 and Width > 100 or Height = 150", dwsData);
        Assert.True(result);

        result = _matcher.Evaluate("Length < 100 and Width < 50", dwsData);
        Assert.False(result);
    }
}
