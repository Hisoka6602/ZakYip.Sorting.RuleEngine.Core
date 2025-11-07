using System.ComponentModel.DataAnnotations;
using ZakYip.Sorting.RuleEngine.Application.DTOs;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Tests.DTOs;

/// <summary>
/// DTOs和实体的边界条件测试
/// Boundary condition tests for DTOs and entities
/// </summary>
public class BoundaryConditionTests
{
    // 常量定义 - 与ParcelProcessRequest和SortingRule的验证属性保持一致
    private const decimal MaxWeightValue = 999999999;
    private const decimal MaxWeightValueExceeded = 1000000000;
    private const int MaxRuleIdLength = 100;
    private const int MaxRuleIdLengthExceeded = 101;
    private const int MaxRuleNameLength = 200;
    private const int MaxConditionExpressionLength = 2000;
    private const int MinPriority = 0;
    private const int MaxPriority = 9999;
    private const int MaxPriorityExceeded = 10000;
    private const int MaxChuteNameLength = 200;
    private const int MaxChuteNameLengthExceeded = 201;

    #region ParcelProcessRequest Boundary Tests

    /// <summary>
    /// 测试最小重量边界值
    /// </summary>
    [Fact]
    public void ParcelProcessRequest_MinimumWeight_PassesValidation()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Weight = 0 // 最小值
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试最大重量边界值
    /// </summary>
    [Fact]
    public void ParcelProcessRequest_MaximumWeight_PassesValidation()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Weight = MaxWeightValue // 最大值
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试超过最大重量
    /// </summary>
    [Fact]
    public void ParcelProcessRequest_WeightExceedsMaximum_FailsValidation()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Weight = MaxWeightValueExceeded // 超过最大值
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("重量"));
    }

    /// <summary>
    /// 测试负重量
    /// </summary>
    [Fact]
    public void ParcelProcessRequest_NegativeWeight_FailsValidation()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Weight = -1
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.ErrorMessage!.Contains("重量"));
    }

    /// <summary>
    /// 测试空条码
    /// </summary>
    [Fact]
    public void ParcelProcessRequest_NullBarcode_PassesValidation()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Barcode = null // 可选字段
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试所有尺寸字段为null
    /// </summary>
    [Fact]
    public void ParcelProcessRequest_AllDimensionsNull_PassesValidation()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Length = null,
            Width = null,
            Height = null,
            Volume = null
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试极大尺寸值
    /// </summary>
    [Fact]
    public void ParcelProcessRequest_ExtremelyLargeDimensions_HandlesCorrectly()
    {
        // Arrange
        var request = new ParcelProcessRequest
        {
            ParcelId = "PKG001",
            CartNumber = "CART001",
            Length = MaxWeightValue,
            Width = MaxWeightValue,
            Height = MaxWeightValue,
            Volume = MaxWeightValue
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    #endregion

    #region SortingRule Boundary Tests

    /// <summary>
    /// 测试规则ID边界长度
    /// </summary>
    [Fact]
    public void SortingRule_RuleIdAtMaxLength_PassesValidation()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = new string('A', MaxRuleIdLength), // 最大长度
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 100",
            TargetChute = "CHUTE-01"
        };

        // Act
        var validationResults = ValidateModel(rule);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试规则ID超过最大长度
    /// </summary>
    [Fact]
    public void SortingRule_RuleIdExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = new string('A', MaxRuleIdLengthExceeded), // 超过最大长度
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 100",
            TargetChute = "CHUTE-01"
        };

        // Act
        var validationResults = ValidateModel(rule);

        // Assert
        Assert.NotEmpty(validationResults);
    }

    /// <summary>
    /// 测试规则优先级最小值
    /// </summary>
    [Fact]
    public void SortingRule_MinimumPriority_PassesValidation()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "RULE-001",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 100",
            TargetChute = "CHUTE-01",
            Priority = MinPriority // 最小优先级
        };

        // Act
        var validationResults = ValidateModel(rule);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试规则优先级最大值
    /// </summary>
    [Fact]
    public void SortingRule_MaximumPriority_PassesValidation()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "RULE-001",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 100",
            TargetChute = "CHUTE-01",
            Priority = MaxPriority // 最大优先级
        };

        // Act
        var validationResults = ValidateModel(rule);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试规则优先级超出范围
    /// </summary>
    [Fact]
    public void SortingRule_PriorityOutOfRange_FailsValidation()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "RULE-001",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 100",
            TargetChute = "CHUTE-01",
            Priority = MaxPriorityExceeded // 超出范围
        };

        // Act
        var validationResults = ValidateModel(rule);

        // Assert
        Assert.NotEmpty(validationResults);
    }

    /// <summary>
    /// 测试负优先级
    /// </summary>
    [Fact]
    public void SortingRule_NegativePriority_FailsValidation()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "RULE-001",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 100",
            TargetChute = "CHUTE-01",
            Priority = -1
        };

        // Act
        var validationResults = ValidateModel(rule);

        // Assert
        Assert.NotEmpty(validationResults);
    }

    /// <summary>
    /// 测试条件表达式最大长度
    /// </summary>
    [Fact]
    public void SortingRule_ConditionExpressionAtMaxLength_PassesValidation()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "RULE-001",
            RuleName = "Test Rule",
            ConditionExpression = new string('A', MaxConditionExpressionLength), // 最大长度
            TargetChute = "CHUTE-01"
        };

        // Act
        var validationResults = ValidateModel(rule);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试空规则描述
    /// </summary>
    [Fact]
    public void SortingRule_NullDescription_PassesValidation()
    {
        // Arrange
        var rule = new SortingRule
        {
            RuleId = "RULE-001",
            RuleName = "Test Rule",
            ConditionExpression = "Weight > 100",
            TargetChute = "CHUTE-01",
            Description = null // 可选字段
        };

        // Act
        var validationResults = ValidateModel(rule);

        // Assert
        Assert.Empty(validationResults);
    }

    #endregion

    #region Chute Boundary Tests

    /// <summary>
    /// 测试格口名称最大长度
    /// </summary>
    [Fact]
    public void Chute_ChuteNameAtMaxLength_PassesValidation()
    {
        // Arrange
        var chute = new Chute
        {
            ChuteName = new string('A', MaxChuteNameLength) // 最大长度
        };

        // Act
        var validationResults = ValidateModel(chute);

        // Assert
        Assert.Empty(validationResults);
    }

    /// <summary>
    /// 测试格口名称超过最大长度
    /// </summary>
    [Fact]
    public void Chute_ChuteNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var chute = new Chute
        {
            ChuteName = new string('A', MaxChuteNameLengthExceeded) // 超过最大长度
        };

        // Act
        var validationResults = ValidateModel(chute);

        // Assert
        Assert.NotEmpty(validationResults);
    }

    /// <summary>
    /// 测试空格口编号
    /// </summary>
    [Fact]
    public void Chute_NullChuteCode_PassesValidation()
    {
        // Arrange
        var chute = new Chute
        {
            ChuteName = "Test Chute",
            ChuteCode = null // 可选字段
        };

        // Act
        var validationResults = ValidateModel(chute);

        // Assert
        Assert.Empty(validationResults);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 验证模型
    /// </summary>
    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    #endregion
}
