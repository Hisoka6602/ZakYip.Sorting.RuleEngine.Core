using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;
using Xunit;
using ZakYip.Sorting.RuleEngine.Domain.Enums;
using ZakYip.Sorting.RuleEngine.Service.Filters;

namespace ZakYip.Sorting.RuleEngine.Tests.Filters;

/// <summary>
/// EnumSchemaFilter单元测试
/// </summary>
public class EnumSchemaFilterTests
{
    [Fact]
    public void Apply_ShouldAddEnumDescriptions_WhenTypeIsEnum()
    {
        // Arrange
        var filter = new EnumSchemaFilter();
        var schema = new OpenApiSchema();
        var context = new SchemaFilterContext(
            typeof(MatchingMethodType),
            new Swashbuckle.AspNetCore.SwaggerGen.SchemaGenerator(
                new Swashbuckle.AspNetCore.SwaggerGen.SchemaGeneratorOptions(),
                new Swashbuckle.AspNetCore.SwaggerGen.JsonSerializerDataContractResolver(
                    new System.Text.Json.JsonSerializerOptions())),
            new Swashbuckle.AspNetCore.SwaggerGen.SchemaRepository());

        // Act
        filter.Apply(schema, context);

        // Assert
        Assert.NotNull(schema.Description);
        Assert.Contains("可选值:", schema.Description);
        Assert.Contains("BarcodeRegex", schema.Description);
        Assert.Contains("条码正则匹配", schema.Description);
        Assert.Equal("integer", schema.Type);
        Assert.Equal("int32", schema.Format);
        Assert.NotEmpty(schema.Enum);
    }

    [Fact]
    public void Apply_ShouldNotModifySchema_WhenTypeIsNotEnum()
    {
        // Arrange
        var filter = new EnumSchemaFilter();
        var schema = new OpenApiSchema { Type = "string" };
        var context = new SchemaFilterContext(
            typeof(string),
            new Swashbuckle.AspNetCore.SwaggerGen.SchemaGenerator(
                new Swashbuckle.AspNetCore.SwaggerGen.SchemaGeneratorOptions(),
                new Swashbuckle.AspNetCore.SwaggerGen.JsonSerializerDataContractResolver(
                    new System.Text.Json.JsonSerializerOptions())),
            new Swashbuckle.AspNetCore.SwaggerGen.SchemaRepository());

        // Act
        filter.Apply(schema, context);

        // Assert
        Assert.Equal("string", schema.Type);
        Assert.Null(schema.Description);
    }

    [Fact]
    public void Apply_ShouldHandleAllEnumValues_ForParcelStatus()
    {
        // Arrange
        var filter = new EnumSchemaFilter();
        var schema = new OpenApiSchema();
        var context = new SchemaFilterContext(
            typeof(ParcelStatus),
            new Swashbuckle.AspNetCore.SwaggerGen.SchemaGenerator(
                new Swashbuckle.AspNetCore.SwaggerGen.SchemaGeneratorOptions(),
                new Swashbuckle.AspNetCore.SwaggerGen.JsonSerializerDataContractResolver(
                    new System.Text.Json.JsonSerializerOptions())),
            new Swashbuckle.AspNetCore.SwaggerGen.SchemaRepository());

        // Act
        filter.Apply(schema, context);

        // Assert
        Assert.NotNull(schema.Description);
        Assert.Contains("待处理", schema.Description);
        Assert.Contains("处理中", schema.Description);
        Assert.Contains("已完成", schema.Description);
        Assert.Contains("失败", schema.Description);
        Assert.Equal(Enum.GetValues(typeof(ParcelStatus)).Length, schema.Enum.Count);
    }
}
