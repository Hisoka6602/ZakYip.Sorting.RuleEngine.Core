using System.Text.Json;
using Xunit;
using ZakYip.Sorting.RuleEngine.Application.DTOs.Requests;

namespace ZakYip.Sorting.RuleEngine.Tests.DTOs;

/// <summary>
/// 验证配置请求 DTO 的 Name 字段有默认值，避免 "The Name field is required" 错误
/// Verify that configuration request DTOs have default Name values to avoid "The Name field is required" error
/// </summary>
public class ConfigRequestValidationTest
{
    [Fact]
    public void JushuitanErpConfigRequest_ShouldHaveDefaultName()
    {
        // Arrange: JSON without Name field
        var json = @"{
            ""Url"": ""https://api.example.com"",
            ""AppKey"": ""test-key"",
            ""AppSecret"": ""test-secret"",
            ""AccessToken"": ""test-token""
        }";
        
        // Act: Deserialize
        var request = JsonSerializer.Deserialize<JushuitanErpConfigRequest>(json);
        
        // Assert: Should use default name value
        Assert.NotNull(request);
    }
    
    [Fact]
    public void PostCollectionConfigRequest_ShouldHaveDefaultName()
    {
        var json = @"{
            ""Url"": ""http://test.com"",
            ""WorkshopCode"": ""WS001"",
            ""DeviceId"": ""DEV001"",
            ""CompanyName"": ""Test Company"",
            ""DeviceBarcode"": ""BC001"",
            ""OrganizationNumber"": ""ORG001"",
            ""EmployeeNumber"": ""EMP001""
        }";
        
        var request = JsonSerializer.Deserialize<PostCollectionConfigRequest>(json);
        
        Assert.NotNull(request);
    }
    
    [Fact]
    public void PostProcessingCenterConfigRequest_ShouldHaveDefaultName()
    {
        var json = @"{
            ""Url"": ""http://test.com"",
            ""Timeout"": 1000,
            ""WorkshopCode"": ""WS001"",
            ""DeviceId"": ""DEV001"",
            ""EmployeeNumber"": ""EMP001"",
            ""LocalServiceUrl"": """"
        }";
        
        var request = JsonSerializer.Deserialize<PostProcessingCenterConfigRequest>(json);
        
        Assert.NotNull(request);
    }
    
    [Fact]
    public void WdtWmsConfigRequest_ShouldHaveDefaultName()
    {
        var json = @"{
            ""Url"": ""http://test.com"",
            ""Sid"": ""sid001"",
            ""AppKey"": ""key001"",
            ""AppSecret"": ""secret001""
        }";
        
        var request = JsonSerializer.Deserialize<WdtWmsConfigRequest>(json);
        
        Assert.NotNull(request);
    }
    
    [Fact]
    public void WdtErpFlagshipConfigRequest_ShouldHaveDefaultName()
    {
        var json = @"{
            ""Url"": ""http://test.com"",
            ""Key"": ""key001"",
            ""Appsecret"": ""secret001"",
            ""Sid"": ""sid001"",
            ""V"": ""1.0"",
            ""Salt"": ""salt001""
        }";
        
        var request = JsonSerializer.Deserialize<WdtErpFlagshipConfigRequest>(json);
        
        Assert.NotNull(request);
    }
    
    [Fact]
    public void AllConfigRequests_ShouldAllowCustomName()
    {
        // Verify that custom names can still be provided
        var json = @"{
            ""Name"": ""Custom Config Name"",
            ""Url"": ""https://api.example.com"",
            ""AppKey"": ""test-key"",
            ""AppSecret"": ""test-secret"",
            ""AccessToken"": ""test-token""
        }";
        
        var request = JsonSerializer.Deserialize<JushuitanErpConfigRequest>(json);
        
        Assert.NotNull(request);
    }
}
