using ZakYip.Sorting.RuleEngine.Application.Services.Matchers;

namespace ZakYip.Sorting.RuleEngine.Tests.Services.Matchers;

/// <summary>
/// API响应匹配器测试
/// </summary>
public class ApiResponseMatcherTests
{
    private readonly ApiResponseMatcher _matcher = new();

    [Theory]
    [InlineData("STRING:success", "{\"status\":\"success\",\"code\":200}", true)]
    [InlineData("STRING:error", "{\"status\":\"success\",\"code\":200}", false)]
    public void Evaluate_StringSearch_ReturnsExpectedResult(string expression, string responseData, bool expected)
    {
        var result = _matcher.Evaluate(expression, responseData);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("REGEX:\\d{3}", "{\"code\":200}", true)]
    [InlineData("REGEX:^success", "success:true", true)]
    [InlineData("REGEX:^error", "success:true", false)]
    public void Evaluate_RegexSearch_ReturnsExpectedResult(string expression, string responseData, bool expected)
    {
        var result = _matcher.Evaluate(expression, responseData);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("JSON:status=success", "{\"status\":\"success\",\"code\":200}", true)]
    [InlineData("JSON:status=error", "{\"status\":\"success\",\"code\":200}", false)]
    [InlineData("JSON:code=200", "{\"status\":\"success\",\"code\":200}", true)]
    public void Evaluate_JsonMatch_ReturnsExpectedResult(string expression, string responseData, bool expected)
    {
        var result = _matcher.Evaluate(expression, responseData);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("JSON:data.user.name=John", "{\"data\":{\"user\":{\"name\":\"John\"}}}", true)]
    [InlineData("JSON:data.user.name=Jane", "{\"data\":{\"user\":{\"name\":\"John\"}}}", false)]
    public void Evaluate_JsonNestedMatch_ReturnsExpectedResult(string expression, string responseData, bool expected)
    {
        var result = _matcher.Evaluate(expression, responseData);
        Assert.Equal(expected, result);
    }
}
