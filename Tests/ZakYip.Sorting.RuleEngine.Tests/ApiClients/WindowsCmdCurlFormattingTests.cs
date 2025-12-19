using Xunit;
using ZakYip.Sorting.RuleEngine.Infrastructure.ApiClients.Shared;

namespace ZakYip.Sorting.RuleEngine.Tests.ApiClients;

/// <summary>
/// Windows CMD Curl 格式化测试
/// Tests for Windows CMD Curl formatting
/// </summary>
public class WindowsCmdCurlFormattingTests
{
    [Fact]
    public void GenerateFormattedCurl_BasicRequest_StartsWithChcp()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url);
        
        // Assert
        Assert.StartsWith("chcp 65001>nul & ", result);
    }

    [Fact]
    public void GenerateFormattedCurl_BasicRequest_IsSingleLine()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };
        var body = "{\"test\":\"value\"}";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, headers, body);
        
        // Assert
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\\", result); // 不应包含续行符
    }

    [Fact]
    public void GenerateFormattedCurl_LessThanSymbol_EscapedCorrectly()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "<xml>test</xml>";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.Contains("^<xml^>test^</xml^>", result);
        Assert.DoesNotContain("<xml>", result); // 原始的 < > 应该被转义
    }

    [Fact]
    public void GenerateFormattedCurl_GreaterThanSymbol_EscapedCorrectly()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "test>output";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.Contains("test^>output", result);
    }

    [Fact]
    public void GenerateFormattedCurl_PipeSymbol_EscapedCorrectly()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "value1|value2";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.Contains("value1^|value2", result);
    }

    [Fact]
    public void GenerateFormattedCurl_DoublePipe_EscapedCorrectly()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "value1||value2";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.Contains("value1^|^|value2", result);
        Assert.DoesNotContain("||", result); // 原始的 || 应该被转义
    }

    [Fact]
    public void GenerateFormattedCurl_AmpersandSymbol_EscapedCorrectly()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "param1&param2";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.Contains("param1^&param2", result);
    }

    [Fact]
    public void GenerateFormattedCurl_DoubleQuotesInXml_EscapedAsDoubleDouble()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "<root xmlns=\"http://example.com\">test</root>";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.Contains("xmlns=\"\"http://example.com\"\"", result);
        Assert.DoesNotContain("xmlns=\"http://example.com\"", result); // 原始双引号应该被转义
    }

    [Fact]
    public void GenerateFormattedCurl_SoapEnvelope_AllEscapesApplied()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/soap";
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "text/xml; charset=utf-8"
        };
        var body = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:web=""http://example.com"">
    <soapenv:Body>
        <web:method>
            <arg0>test||data&more</arg0>
        </web:method>
    </soapenv:Body>
</soapenv:Envelope>";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, headers, body);
        
        // Assert
        Assert.StartsWith("chcp 65001>nul & ", result);
        Assert.Contains("^<soapenv:Envelope", result);
        Assert.Contains("xmlns:soapenv=\"\"http://schemas.xmlsoap.org/soap/envelope/\"\"", result);
        Assert.Contains("xmlns:web=\"\"http://example.com\"\"", result);
        Assert.Contains("^<arg0^>test^|^|data^&more^</arg0^>", result);
        Assert.Contains("^</soapenv:Envelope^>", result);
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\\", result);
    }

    [Fact]
    public void GenerateFormattedCurl_ChineseCharacters_IncludedCorrectly()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "{\"message\":\"测试中文\"}";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.StartsWith("chcp 65001>nul & ", result); // 确保有 chcp
        Assert.Contains("测试中文", result); // 中文字符应该保留
    }

    [Fact]
    public void GenerateFormattedCurl_MultipleHeaders_AllIncluded()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Authorization"] = "Bearer token123",
            ["X-Custom-Header"] = "custom-value"
        };
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, headers);
        
        // Assert
        Assert.Contains("-H \"Content-Type: application/json\"", result);
        Assert.Contains("-H \"Authorization: Bearer token123\"", result);
        Assert.Contains("-H \"X-Custom-Header: custom-value\"", result);
    }

    [Fact]
    public void GenerateFormattedCurl_GetMethod_NoBody()
    {
        // Arrange
        var method = "GET";
        var url = "http://example.com/api";
        var headers = new Dictionary<string, string>
        {
            ["Accept"] = "application/json"
        };
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, headers);
        
        // Assert
        Assert.StartsWith("chcp 65001>nul & ", result);
        Assert.Contains("curl -X GET", result);
        Assert.Contains("-H \"Accept: application/json\"", result);
        Assert.DoesNotContain("--data-raw", result);
    }

    [Fact]
    public void GenerateFormattedCurl_EmptyBody_NoDataRawParameter()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.DoesNotContain("--data-raw", result);
    }

    [Fact]
    public void GenerateFormattedCurl_NullBody_NoDataRawParameter()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: null);
        
        // Assert
        Assert.DoesNotContain("--data-raw", result);
    }

    [Fact]
    public void GenerateFormattedCurl_ComplexXmlWithAllSpecialChars_AllEscaped()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = @"<root xmlns=""http://ns.com"">
    <data>&lt;test&gt; || special &amp; chars</data>
    <pipe>value1|value2||value3</pipe>
</root>";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        // 验证所有转义
        Assert.Contains("^<root xmlns=\"\"http://ns.com\"\"^>", result);
        Assert.Contains("^<data^>", result);
        Assert.Contains("^&lt;test^&gt;", result);
        Assert.Contains("^|^|", result); // || 应该被转义为 ^|^|
        Assert.Contains("^&amp;", result);
        Assert.Contains("^</root^>", result);
        // 验证没有换行符
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("\r", result);
    }

    [Fact]
    public void GenerateFormattedCurl_UrlContainsSpecialChars_UrlNotEscaped()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api?param1=value1&param2=value2";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url);
        
        // Assert
        // URL 应该在双引号内，但不应该被转义（URL 参数的 & 不需要转义）
        Assert.Contains($"curl -X POST \"{url}\"", result);
    }

    [Fact]
    public void GenerateFormattedCurl_JsonBody_OnlyQuotesEscaped()
    {
        // Arrange
        var method = "POST";
        var url = "http://example.com/api";
        var body = "{\"name\":\"John\",\"age\":30}";
        
        // Act
        var result = ApiRequestHelper.GenerateFormattedCurl(method, url, body: body);
        
        // Assert
        Assert.Contains("--data-raw \"{\"\"name\"\":\"\"John\"\",\"\"age\"\":30}\"", result);
        // JSON 中没有 <, >, |, & 这些特殊字符，所以不应该出现 ^ 转义
        Assert.DoesNotContain("^<", result);
        Assert.DoesNotContain("^>", result);
    }
}
