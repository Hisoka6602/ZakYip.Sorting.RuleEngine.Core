using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Entities;
using ZakYip.Sorting.RuleEngine.Infrastructure.Services;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure.Services;

/// <summary>
/// DWS数据解析器测试
/// DWS data parser tests
/// </summary>
public class DwsDataParserTests
{
    private readonly DwsDataParser _parser;

    public DwsDataParserTests()
    {
        _parser = new DwsDataParser(new Mocks.MockSystemClock());
    }

    [Fact]
    public void Parse_WithTemplateFormat_ShouldParseCorrectly()
    {
        // Arrange
        var rawData = "9811962888027,0.000,0,0,0,0,1765365164205";
        var template = new DwsDataTemplate
        {
            TemplateId = 1L,
            Name = "Standard Template",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
        };

        // Act
        var result = _parser.Parse(rawData, template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("9811962888027", result.Barcode);
        Assert.Equal(0.000m, result.Weight);
        Assert.Equal(0m, result.Length);
        Assert.Equal(0m, result.Width);
        Assert.Equal(0m, result.Height);
        Assert.Equal(0m, result.Volume);
        // Timestamp should be parsed from Unix milliseconds
        Assert.NotEqual(default, result.ScannedAt);
    }

    [Fact]
    public void Parse_WithDecimalValues_ShouldParseCorrectly()
    {
        // Arrange
        var rawData = "TEST123,250.5,100.2,50.3,30.1,150.75,1765365164205";
        var template = new DwsDataTemplate
        {
            TemplateId = 1L,
            Name = "Standard Template",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
        };

        // Act
        var result = _parser.Parse(rawData, template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TEST123", result.Barcode);
        Assert.Equal(250.5m, result.Weight);
        Assert.Equal(100.2m, result.Length);
        Assert.Equal(50.3m, result.Width);
        Assert.Equal(30.1m, result.Height);
        Assert.Equal(150.75m, result.Volume);
    }

    [Fact]
    public void Parse_WithPartialTemplate_ShouldParseAvailableFields()
    {
        // Arrange
        var rawData = "ABC456,123.45,200.0";
        var template = new DwsDataTemplate
        {
            TemplateId = 2L,
            Name = "Partial Template",
            Template = "{Code},{Weight},{Length}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
        };

        // Act
        var result = _parser.Parse(rawData, template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ABC456", result.Barcode);
        Assert.Equal(123.45m, result.Weight);
        Assert.Equal(200.0m, result.Length);
        Assert.Equal(0m, result.Width);
        Assert.Equal(0m, result.Height);
        Assert.Equal(0m, result.Volume);
    }

    [Fact]
    public void Parse_WithJsonFormat_ShouldParseJson()
    {
        // Arrange
        var jsonData = "{\"barcode\":\"JSON123\",\"weight\":99.9,\"length\":150,\"width\":80,\"height\":60,\"volume\":720}";
        var template = new DwsDataTemplate
        {
            TemplateId = 3L,
            Name = "JSON Template",
            Template = "", // Not used for JSON
            IsJsonFormat = true,
            IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
        };

        // Act
        var result = _parser.Parse(jsonData, template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("JSON123", result.Barcode);
        Assert.Equal(99.9m, result.Weight);
        Assert.Equal(150m, result.Length);
        Assert.Equal(80m, result.Width);
        Assert.Equal(60m, result.Height);
        Assert.Equal(720m, result.Volume);
    }

    [Fact]
    public void Parse_WithInvalidData_ShouldReturnNull()
    {
        // Arrange
        var rawData = "INVALID,DATA";
        var template = new DwsDataTemplate
        {
            TemplateId = 1L,
            Name = "Standard Template",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
        };

        // Act
        var result = _parser.Parse(rawData, template);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithEmptyData_ShouldReturnNull()
    {
        // Arrange
        var rawData = "";
        var template = new DwsDataTemplate
        {
            TemplateId = 1L,
            Name = "Standard Template",
            Template = "{Code},{Weight},{Length},{Width},{Height},{Volume},{Timestamp}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
        };

        // Act
        var result = _parser.Parse(rawData, template);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithCustomDelimiter_ShouldParseCorrectly()
    {
        // Arrange
        var rawData = "PIPE123|456.78|100|50|25|125.0|1765365164205";
        var template = new DwsDataTemplate
        {
            TemplateId = 4L,
            Name = "Pipe Delimited Template",
            Template = "{Code}|{Weight}|{Length}|{Width}|{Height}|{Volume}|{Timestamp}",
            Delimiter = "|",
            IsJsonFormat = false,
            IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
        };

        // Act
        var result = _parser.Parse(rawData, template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PIPE123", result.Barcode);
        Assert.Equal(456.78m, result.Weight);
        Assert.Equal(100m, result.Length);
        Assert.Equal(50m, result.Width);
        Assert.Equal(25m, result.Height);
        Assert.Equal(125.0m, result.Volume);
    }

    [Fact]
    public void Parse_WithBarcodeFieldName_ShouldParseAsBarcode()
    {
        // Arrange
        var rawData = "BARCODE789,350.25,180";
        var template = new DwsDataTemplate
        {
            TemplateId = 5L,
            Name = "Barcode Template",
            Template = "{Barcode},{Weight},{Length}",
            Delimiter = ",",
            IsJsonFormat = false,
            IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
        };

        // Act
        var result = _parser.Parse(rawData, template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BARCODE789", result.Barcode);
        Assert.Equal(350.25m, result.Weight);
        Assert.Equal(180m, result.Length);
    }
}
