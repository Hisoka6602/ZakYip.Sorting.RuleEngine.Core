using ZakYip.Sorting.RuleEngine.Application.Services;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Domain.Enums;

namespace ZakYip.Sorting.RuleEngine.Tests.Services;

/// <summary>
/// 规则验证服务单元测试
/// Unit tests for RuleValidationService
/// </summary>
public class RuleValidationServiceTests
{
    private readonly RuleValidationService _service;

    public RuleValidationServiceTests()
    {
        _service = new RuleValidationService();
    }

    [Fact]
    public void ValidateRule_ValidRule_ReturnsTrue()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Valid Rule",
            ConditionExpression = "Weight > 1000",
            TargetChute = "CHUTE-A01",
            Priority = 1,
            IsEnabled = true,
            MatchingMethod = MatchingMethodType.WeightMatch
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateRule_EmptyRuleId_ReturnsFalse()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 1000",
            TargetChute = "CHUTE-A01",
            Priority = 1
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("规则ID不能为空", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRule_EmptyRuleName_ReturnsFalse()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "",
            ConditionExpression = "Weight > 1000",
            TargetChute = "CHUTE-A01",
            Priority = 1
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("规则名称不能为空", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRule_EmptyTargetChute_ReturnsFalse()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 1000",
            TargetChute = "",
            Priority = 1
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("目标格口不能为空", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRule_EmptyConditionExpression_ReturnsFalse()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Test Rule",
            ConditionExpression = "",
            TargetChute = "CHUTE-A01",
            Priority = 1
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("条件表达式不能为空", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRule_ExpressionTooLong_ReturnsFalse()
    {
        // Arrange
        var longExpression = new string('a', 2001);
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Test Rule",
            ConditionExpression = longExpression,
            TargetChute = "CHUTE-A01",
            Priority = 1
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("条件表达式长度不能超过2000个字符", result.ErrorMessage);
    }

    [Theory]
    [InlineData("eval(malicious_code)")]
    [InlineData("System.Runtime.Execute")]
    [InlineData("DROP TABLE users")]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert(1)")]
    public void ValidateRule_DangerousKeywords_ReturnsFalse(string dangerousExpression)
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Dangerous Rule",
            ConditionExpression = dangerousExpression,
            TargetChute = "CHUTE-A01",
            Priority = 1,
            MatchingMethod = MatchingMethodType.LegacyExpression
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("危险关键字", result.ErrorMessage);
    }

    [Theory]
    [InlineData("Weight > 1000; DROP TABLE", "危险关键字")]
    [InlineData("Weight > 1000 | test", "非法字符")]
    [InlineData("Weight > 1000 & malicious", "非法字符")]
    [InlineData("Weight > 1000 ` echo test", "非法字符")]
    public void ValidateRule_IllegalCharacters_ReturnsFalse(string expression, string expectedErrorSubstring)
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Test Rule",
            ConditionExpression = expression,
            TargetChute = "CHUTE-A01",
            Priority = 1,
            MatchingMethod = MatchingMethodType.WeightMatch
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(expectedErrorSubstring, result.ErrorMessage);
    }

    [Fact]
    public void ValidateRule_InvalidPriorityNegative_ReturnsFalse()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 1000",
            TargetChute = "CHUTE-A01",
            Priority = -1,
            MatchingMethod = MatchingMethodType.WeightMatch
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("优先级必须在0到9999之间", result.ErrorMessage);
    }

    [Fact]
    public void ValidateRule_InvalidPriorityTooHigh_ReturnsFalse()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 1000",
            TargetChute = "CHUTE-A01",
            Priority = 10000,
            MatchingMethod = MatchingMethodType.WeightMatch
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("优先级必须在0到9999之间", result.ErrorMessage);
    }

    [Theory]
    [InlineData("Weight > 1000")]
    [InlineData("Weight >= 500 and Weight <= 2000")]
    [InlineData("Weight > 100 or Weight < 50")]
    public void ValidateRule_ValidWeightExpression_ReturnsTrue(string expression)
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Weight Rule",
            ConditionExpression = expression,
            TargetChute = "CHUTE-A01",
            Priority = 1,
            MatchingMethod = MatchingMethodType.WeightMatch
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("STARTSWITH:SF")]
    [InlineData("CONTAINS:EXPRESS")]
    [InlineData("NOTCONTAINS:FRAGILE")]
    [InlineData("ALLDIGITS:")]
    [InlineData("ALPHANUMERIC:")]
    [InlineData("LENGTH:10")]
    [InlineData("REGEX:^[A-Z]{2}\\d{8}$")]
    public void ValidateRule_ValidBarcodeExpression_ReturnsTrue(string expression)
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Barcode Rule",
            ConditionExpression = expression,
            TargetChute = "CHUTE-A01",
            Priority = 1,
            MatchingMethod = MatchingMethodType.BarcodeRegex
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("Volume > 5000")]
    [InlineData("Length > 100 and Width > 50")]
    [InlineData("Height < 200 or Volume > 10000")]
    public void ValidateRule_ValidVolumeExpression_ReturnsTrue(string expression)
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "Volume Rule",
            ConditionExpression = expression,
            TargetChute = "CHUTE-A01",
            Priority = 1,
            MatchingMethod = MatchingMethodType.VolumeMatch
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("STRING:Expected Response")]
    [InlineData("STRINGREVERSE:Response Expected")]
    [InlineData("REGEX:^\\d{4}-\\d{2}$")]
    [InlineData("JSON:$.data.status")]
    public void ValidateRule_ValidApiResponseExpression_ReturnsTrue(string expression)
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "R1",
            RuleName = "API Rule",
            ConditionExpression = expression,
            TargetChute = "CHUTE-A01",
            Priority = 1,
            MatchingMethod = MatchingMethodType.ApiResponseMatch
        };

        // Act
        var result = _service.ValidateRule(rule);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateRules_MultipleRules_ReturnsCorrectResults()
    {
        // Arrange
        var rules = new List<SortingRule>
        {
            new SortingRule
            {
                RuleId = "R1",
                RuleName = "Valid Rule",
                ConditionExpression = "Weight > 1000",
                TargetChute = "CHUTE-A01",
                Priority = 1,
                MatchingMethod = MatchingMethodType.WeightMatch
            },
            new SortingRule
            {
                RuleId = "R2",
                RuleName = "",
                ConditionExpression = "Weight > 500",
                TargetChute = "CHUTE-A02",
                Priority = 2,
                MatchingMethod = MatchingMethodType.WeightMatch
            },
            new SortingRule
            {
                RuleId = "R3",
                RuleName = "Another Valid Rule",
                ConditionExpression = "STARTSWITH:SF",
                TargetChute = "CHUTE-B01",
                Priority = 3,
                MatchingMethod = MatchingMethodType.BarcodeRegex
            }
        };

        // Act
        var results = _service.ValidateRules(rules);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results["R1"].IsValid);
        Assert.False(results["R2"].IsValid);
        Assert.Equal("规则名称不能为空", results["R2"].ErrorMessage);
        Assert.True(results["R3"].IsValid);
    }
}
