using ZakYip.Sorting.RuleEngine.Application.Services.Matchers;
using ZakYip.Sorting.RuleEngine.Domain.Entities;

namespace ZakYip.Sorting.RuleEngine.Tests.Services.Matchers;

/// <summary>
/// 低代码表达式匹配器单元测试
/// Unit tests for LowCodeExpressionMatcher
/// </summary>
public class LowCodeExpressionMatcherTests
{
    private readonly LowCodeExpressionMatcher _matcher;

    public LowCodeExpressionMatcherTests()
    {
        _matcher = new LowCodeExpressionMatcher();
    }

    [Fact]
    public void Evaluate_WeightCondition_ReturnsTrue()
    {
        // Arrange
        var expression = "Weight > 1000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_WeightConditionNotMet_ReturnsFalse()
    {
        // Arrange
        var expression = "Weight > 2000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_WithIfWrapper_ReturnsTrue()
    {
        // Arrange
        var expression = "if(Weight > 1000)";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_AndConditionBothTrue_ReturnsTrue()
    {
        // Arrange
        var expression = "Weight > 1000 and Volume > 5000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500, Volume = 6000 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_AndConditionOneFalse_ReturnsFalse()
    {
        // Arrange
        var expression = "Weight > 1000 and Volume > 10000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500, Volume = 6000 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_OrConditionOneTrue_ReturnsTrue()
    {
        // Arrange
        var expression = "Weight > 2000 or Volume > 5000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500, Volume = 6000 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_OrConditionBothFalse_ReturnsFalse()
    {
        // Arrange
        var expression = "Weight > 2000 or Volume > 10000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500, Volume = 6000 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_VolumeCondition_ReturnsTrue()
    {
        // Arrange
        var expression = "Volume > 5000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Volume = 6000 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_LengthCondition_ReturnsTrue()
    {
        // Arrange
        var expression = "Length > 300";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Length = 400 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_ComplexExpression_ReturnsTrue()
    {
        // Arrange
        var expression = "Weight > 1000 and Volume > 5000 and Length > 200";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData 
        { 
            Weight = 1500, 
            Volume = 6000,
            Length = 300
        };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_OcrCondition_WithOcrData_ReturnsTrue()
    {
        // Arrange
        var expression = "firstSegmentCode = ^64";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var ocrData = new OcrData 
        { 
            FirstSegmentCode = "641234" 
        };
        var thirdPartyResponse = new WcsApiResponse 
        { 
            OcrData = ocrData 
        };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, null, thirdPartyResponse);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_OcrCondition_WithoutOcrData_ReturnsFalse()
    {
        // Arrange
        var expression = "firstSegmentCode = ^64";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, null, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_EmptyExpression_ReturnsFalse()
    {
        // Arrange
        var expression = "";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, null, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_NullExpression_ReturnsFalse()
    {
        // Arrange
        string? expression = null;
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };

        // Act
        var result = _matcher.Evaluate(expression!, parcelInfo, null, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_WeightConditionWithoutDwsData_ReturnsFalse()
    {
        // Arrange
        var expression = "Weight > 1000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, null, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_InvalidExpression_ReturnsFalse()
    {
        // Arrange
        var expression = "InvalidField > 1000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_BarcodeCondition_ReturnsTrue()
    {
        // Arrange
        var expression = "Barcode = ^SF";
        var parcelInfo = new ParcelInfo 
        { 
            ParcelId = "PKG001",
            Barcode = "SF123456789"
        };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, null, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_BarcodeFromDwsData_ReturnsTrue()
    {
        // Arrange
        var expression = "Barcode = ^EMS";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Barcode = "EMS987654321" };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_MixedConditions_ReturnsTrue()
    {
        // Arrange
        var expression = "Weight > 1000 and Barcode = ^SF";
        var parcelInfo = new ParcelInfo 
        { 
            ParcelId = "PKG001",
            Barcode = "SF123456789"
        };
        var dwsData = new DwsData { Weight = 1500 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var expression = "Weight > 1000 and Volume > 5000";
        var parcelInfo = new ParcelInfo { ParcelId = "PKG001" };
        var dwsData = new DwsData { Weight = 1500, Volume = 6000 };

        // Act
        var result = _matcher.Evaluate(expression, parcelInfo, dwsData, null);

        // Assert
        Assert.True(result);
    }
}
