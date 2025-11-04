using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ZakYip.Sorting.RuleEngine.Infrastructure.Persistence.Optimizations;

namespace ZakYip.Sorting.RuleEngine.Tests.Infrastructure;

/// <summary>
/// 查询优化扩展测试
/// Query optimization extensions tests
/// </summary>
public class QueryOptimizationExtensionsTests
{
    private readonly Mock<ILogger> _mockLogger;

    public QueryOptimizationExtensionsTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void GetOptimizationSuggestions_FastQuery_ReturnNoOptimizationNeeded()
    {
        // Arrange
        var executionTimeMs = 500L;
        var recordCount = 100;
        var queryString = "SELECT Id, Name FROM Users WHERE IsActive = 1";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("查询性能良好", suggestions);
    }

    [Fact]
    public void GetOptimizationSuggestions_SlowQuery_ReturnOptimizationWarning()
    {
        // Arrange
        var executionTimeMs = 2000L; // 2 seconds
        var recordCount = 100;
        var queryString = "SELECT * FROM Users";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("建议优化", suggestions);
    }

    [Fact]
    public void GetOptimizationSuggestions_VerySlowQuery_ReturnStrongWarning()
    {
        // Arrange
        var executionTimeMs = 6000L; // 6 seconds
        var recordCount = 100;
        var queryString = "SELECT * FROM Users";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("强烈建议优化", suggestions);
    }

    [Fact]
    public void GetOptimizationSuggestions_TooManyRecords_ReturnPagingWarning()
    {
        // Arrange
        var executionTimeMs = 500L;
        var recordCount = 15000;
        var queryString = "SELECT Id, Name FROM Users";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("返回记录数超过10000条", suggestions);
        Assert.Contains("分页", suggestions);
    }

    [Fact]
    public void GetOptimizationSuggestions_SelectAll_ReturnSelectWarning()
    {
        // Arrange
        var executionTimeMs = 500L;
        var recordCount = 100;
        var queryString = "SELECT * FROM Users";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("SELECT *", suggestions);
        Assert.Contains("只选择需要的列", suggestions);
    }

    [Fact]
    public void GetOptimizationSuggestions_NoWhereClause_ReturnWhereWarning()
    {
        // Arrange
        var executionTimeMs = 500L;
        var recordCount = 100;
        var queryString = "SELECT Id, Name FROM Users";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("可能缺少WHERE条件", suggestions);
        Assert.Contains("过滤条件", suggestions);
    }

    [Fact]
    public void GetOptimizationSuggestions_OrCondition_ReturnOrWarning()
    {
        // Arrange
        var executionTimeMs = 500L;
        var recordCount = 100;
        var queryString = "SELECT Id, Name FROM Users WHERE Status = 1 OR Status = 2";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("OR条件", suggestions);
        Assert.Contains("IN或UNION", suggestions);
    }

    [Fact]
    public void GetOptimizationSuggestions_MultipleIssues_ReturnMultipleSuggestions()
    {
        // Arrange
        var executionTimeMs = 6000L;
        var recordCount = 15000;
        var queryString = "SELECT * FROM Users";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("强烈建议优化", suggestions);
        Assert.Contains("返回记录数超过10000条", suggestions);
        Assert.Contains("SELECT *", suggestions);
        Assert.Contains("缺少WHERE条件", suggestions);
    }

    [Fact]
    public void GetOptimizationSuggestions_OptimizedQuery_MinimalSuggestions()
    {
        // Arrange
        var executionTimeMs = 200L;
        var recordCount = 50;
        var queryString = "SELECT Id, Name, Email FROM Users WHERE IsActive = 1 AND CreatedAt > @date";

        // Act
        var suggestions = QueryOptimizationExtensions.GetOptimizationSuggestions(
            executionTimeMs, recordCount, queryString);

        // Assert
        Assert.Contains("查询性能良好", suggestions);
        Assert.DoesNotContain("强烈", suggestions);
        Assert.DoesNotContain("返回记录数超过", suggestions);
    }
}
